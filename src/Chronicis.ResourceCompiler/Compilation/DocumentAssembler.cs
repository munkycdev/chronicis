using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class DocumentAssembler
{
    private readonly OrderingService _orderingService = new();
    private readonly RecursionGuard _recursionGuard = new();

    public Task<CompilationResult> AssembleAsync(
        Manifest.Models.Manifest manifest,
        RawLoadResult rawLoadResult,
        IndexBuildResult indexBuildResult,
        CompilerOptions options,
        CancellationToken cancellationToken)
    {
        var warnings = new WarningSink();
        var rawByEntity = rawLoadResult.EntitySets.ToDictionary(set => set.EntityName, StringComparer.Ordinal);
        var pkKeyLookup = BuildPkKeyLookup(indexBuildResult.PkIndexes);
        var fkIndexLookup = BuildFkIndexLookup(indexBuildResult.FkIndexes);

        var documents = new List<CompiledDocument>();
        foreach (var entity in manifest.Entities.Values
                     .Where(entity => entity.IsRoot)
                     .OrderBy(entity => entity.Name, StringComparer.Ordinal))
        {
            if (!rawByEntity.TryGetValue(entity.Name, out var rawSet))
            {
                continue;
            }

            if (!pkKeyLookup.TryGetValue(entity.Name, out var keysByRow))
            {
                continue;
            }

            var orderedRows = _orderingService.ApplyOrder(rawSet.Rows, entity.OrderBy, entity.Name, warnings);
            foreach (var row in orderedRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!keysByRow.TryGetValue(row, out var key))
                {
                    continue;
                }

                if (!_recursionGuard.TryEnter(entity.Name, key, 0))
                {
                    warnings.Add(new Warning(
                        WarningCode.CycleDetected,
                        WarningSeverity.Warning,
                        $"Cycle detected while assembling entity '{entity.Name}'.",
                        entity.Name));
                    continue;
                }

                try
                {
                    var payload = AssemblePayload(entity, row, key, 0, manifest, rawByEntity, pkKeyLookup, fkIndexLookup, options, warnings, cancellationToken);
                    documents.Add(new CompiledDocument(entity.Name, key, payload));
                }
                finally
                {
                    _recursionGuard.Exit(entity.Name, key);
                }
            }
        }

        var result = new CompilationResult(documents, warnings.Warnings);
        return Task.FromResult(result);
    }

    private JsonObject AssemblePayload(
        ManifestEntity entity,
        RawEntityRow row,
        KeyValue key,
        int depth,
        Manifest.Models.Manifest manifest,
        IReadOnlyDictionary<string, RawEntitySet> rawByEntity,
        IReadOnlyDictionary<string, Dictionary<RawEntityRow, KeyValue>> pkKeyLookup,
        IReadOnlyDictionary<string, FkIndex> fkIndexLookup,
        CompilerOptions options,
        WarningSink warnings,
        CancellationToken cancellationToken)
    {
        var payload = CopyObject(row.Data);

        foreach (var child in entity.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var childName = string.IsNullOrWhiteSpace(child.As) ? child.Entity : child.As;
            if (string.IsNullOrWhiteSpace(childName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(child.Entity))
            {
                payload[childName] = new JsonArray();
                continue;
            }

            var fkKey = $"{child.Entity}|{child.ForeignKeyField}";
            if (!fkIndexLookup.TryGetValue(fkKey, out var fkIndex))
            {
                payload[childName] = new JsonArray();
                continue;
            }

            if (!fkIndex.RowsByKey.TryGetValue(key, out var childRows))
            {
                payload[childName] = new JsonArray();
                continue;
            }

            if (!pkKeyLookup.TryGetValue(child.Entity, out var childKeysByRow))
            {
                payload[childName] = new JsonArray();
                continue;
            }

            var orderedChildren = _orderingService.ApplyOrder(childRows, child.OrderBy, child.Entity, warnings);
            var array = new JsonArray();

            foreach (var childRow in orderedChildren)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!childKeysByRow.TryGetValue(childRow, out var childKey))
                {
                    continue;
                }

                var nextDepth = depth + 1;
                var maxDepth = child.MaxDepth ?? options.MaxDepth;
                if (nextDepth > maxDepth)
                {
                    warnings.Add(new Warning(
                        WarningCode.MaxDepthExceeded,
                        WarningSeverity.Warning,
                        $"Max depth exceeded at entity '{child.Entity}' with key '{childKey.CanonicalValue}'.",
                        child.Entity));
                    continue;
                }

                if (!_recursionGuard.TryEnter(child.Entity, childKey, nextDepth))
                {
                    warnings.Add(new Warning(
                        WarningCode.CycleDetected,
                        WarningSeverity.Warning,
                        $"Cycle detected at entity '{child.Entity}' with key '{childKey.CanonicalValue}'.",
                        child.Entity));
                    continue;
                }

                try
                {
                    var childEntity = manifest.Entities[child.Entity];
                    var childPayload = AssemblePayload(
                        childEntity,
                        childRow,
                        childKey,
                        nextDepth,
                        manifest,
                        rawByEntity,
                        pkKeyLookup,
                        fkIndexLookup,
                        options,
                        warnings,
                        cancellationToken);
                    array.Add(childPayload);
                }
                finally
                {
                    _recursionGuard.Exit(child.Entity, childKey);
                }
            }

            payload[childName] = array;
        }

        return payload;
    }

    private static JsonObject CopyObject(JsonElement element)
    {
        var obj = new JsonObject();
        if (element.ValueKind != JsonValueKind.Object)
        {
            return obj;
        }

        foreach (var property in element.EnumerateObject())
        {
            obj[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        return obj;
    }

    private static IReadOnlyDictionary<string, Dictionary<RawEntityRow, KeyValue>> BuildPkKeyLookup(
        IReadOnlyDictionary<string, PkIndex> pkIndexes)
    {
        var lookup = new Dictionary<string, Dictionary<RawEntityRow, KeyValue>>(StringComparer.Ordinal);
        foreach (var pair in pkIndexes)
        {
            var map = new Dictionary<RawEntityRow, KeyValue>();
            foreach (var entry in pair.Value.RowsByKey)
            {
                map[entry.Value] = entry.Key;
            }

            lookup[pair.Key] = map;
        }

        return lookup;
    }

    private static IReadOnlyDictionary<string, FkIndex> BuildFkIndexLookup(IReadOnlyList<FkIndex> fkIndexes)
    {
        var lookup = new Dictionary<string, FkIndex>(StringComparer.Ordinal);
        foreach (var index in fkIndexes)
        {
            var key = $"{index.EntityName}|{index.FieldName}";
            if (!lookup.ContainsKey(key))
            {
                lookup[key] = index;
            }
        }

        return lookup;
    }
}
