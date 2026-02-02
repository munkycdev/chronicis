using System.Text.Json;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Indexing;

public sealed class IndexBuilder
{
    private readonly KeyCanonicalizer _canonicalizer;

    public IndexBuilder()
    {
        _canonicalizer = new KeyCanonicalizer();
    }

    public IndexBuilder(KeyCanonicalizer canonicalizer)
    {
        _canonicalizer = canonicalizer;
    }

    public IndexBuildResult BuildIndexes(Manifest.Models.Manifest manifest, RawLoadResult rawLoadResult)
    {
        var warnings = new List<Warning>();
        var pkIndexes = new Dictionary<string, PkIndex>(StringComparer.Ordinal);
        var fkIndexes = new List<FkIndex>();

        var rawByEntity = rawLoadResult.EntitySets
            .ToDictionary(entitySet => entitySet.EntityName, StringComparer.Ordinal);

        foreach (var pair in manifest.Entities.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!rawByEntity.TryGetValue(pair.Key, out var rawEntitySet))
            {
                continue;
            }

            var pkIndex = BuildPkIndex(pair.Key, pair.Value, rawEntitySet, warnings);
            pkIndexes[pair.Key] = pkIndex;
        }

        var fkIndexKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parent in manifest.Entities.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (var child in parent.Value.Children)
            {
                var key = $"{child.Entity}|{child.ForeignKeyField}";
                if (!fkIndexKeys.Add(key))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(child.Entity))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Entity '{parent.Key}' defines a child relationship with no entity name.",
                        parent.Key));
                    continue;
                }

                if (!rawByEntity.TryGetValue(child.Entity, out var childSet))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Entity '{parent.Key}' references missing child entity '{child.Entity}'.",
                        parent.Key));
                    continue;
                }

                var fkIndex = BuildFkIndex(parent.Key, child, childSet, warnings);
                fkIndexes.Add(fkIndex);
            }
        }

        return new IndexBuildResult(pkIndexes, fkIndexes, warnings);
    }

    public Task<PkIndex> BuildPkIndexAsync(ManifestEntity entity, RawEntitySet rawEntitySet, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var warnings = new List<Warning>();
        var pkIndex = BuildPkIndex(entity.Name, entity, rawEntitySet, warnings);
        return Task.FromResult(pkIndex);
    }

    public Task<FkIndex> BuildFkIndexAsync(string parentEntityName, ManifestChild relationship, RawEntitySet rawEntitySet, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var warnings = new List<Warning>();
        var fkIndex = BuildFkIndex(parentEntityName, relationship, rawEntitySet, warnings);
        return Task.FromResult(fkIndex);
    }

    private PkIndex BuildPkIndex(string entityName, ManifestEntity entity, RawEntitySet rawEntitySet, List<Warning> warnings)
    {
        var rowsByKey = new Dictionary<KeyValue, RawEntityRow>();

        if (string.IsNullOrWhiteSpace(entity.PrimaryKey))
        {
            warnings.Add(new Warning(
                WarningCode.MissingPk,
                WarningSeverity.Error,
                $"Entity '{entityName}' has no primary key configured.",
                entityName));
            return new PkIndex(entityName, rowsByKey);
        }

        foreach (var row in rawEntitySet.Rows.OrderBy(row => row.RowIndex))
        {
            if (!row.ExtractedPkElement.HasValue)
            {
                warnings.Add(new Warning(
                    WarningCode.MissingPk,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' is missing PK '{entity.PrimaryKey}' at row {row.RowIndex}.",
                    entityName,
                    $"$[{row.RowIndex}].{entity.PrimaryKey}"));
                continue;
            }

            var pkElement = row.ExtractedPkElement.Value;
            if (pkElement.ValueKind is JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Undefined)
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidPkType,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' has an invalid PK type at row {row.RowIndex}.",
                    entityName,
                    $"$[{row.RowIndex}].{entity.PrimaryKey}"));
                continue;
            }

            if (!_canonicalizer.TryCanonicalize(pkElement, out var key, out _))
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidPkType,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' has an invalid PK type at row {row.RowIndex}.",
                    entityName,
                    $"$[{row.RowIndex}].{entity.PrimaryKey}"));
                continue;
            }

            if (rowsByKey.ContainsKey(key))
            {
                warnings.Add(new Warning(
                    WarningCode.DuplicatePk,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' has duplicate PK '{key.CanonicalValue}' at row {row.RowIndex}.",
                    entityName,
                    $"$[{row.RowIndex}].{entity.PrimaryKey}"));
                continue;
            }

            rowsByKey[key] = row;
        }

        return new PkIndex(entityName, rowsByKey);
    }

    private FkIndex BuildFkIndex(string parentEntityName, ManifestChild relationship, RawEntitySet rawEntitySet, List<Warning> warnings)
    {
        var rowsByKey = new Dictionary<KeyValue, List<RawEntityRow>>();
        var fieldName = relationship.ForeignKeyField ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            warnings.Add(new Warning(
                WarningCode.MissingFk,
                WarningSeverity.Error,
                $"Entity '{parentEntityName}' defines a child relationship without a foreign key field.",
                parentEntityName));
            return new FkIndex(
                parentEntityName,
                rawEntitySet.EntityName,
                fieldName,
                new Dictionary<KeyValue, IReadOnlyList<RawEntityRow>>());
        }

        foreach (var row in rawEntitySet.Rows.OrderBy(row => row.RowIndex))
        {
            if (!row.Data.TryGetProperty(fieldName, out var fkElement))
            {
                warnings.Add(new Warning(
                    WarningCode.MissingFk,
                    WarningSeverity.Warning,
                    $"Missing FK '{fieldName}' at row {row.RowIndex} for entity '{rawEntitySet.EntityName}'.",
                    rawEntitySet.EntityName,
                    $"$[{row.RowIndex}].{fieldName}"));
                continue;
            }

            if (fkElement.ValueKind is JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Undefined)
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidFkType,
                    WarningSeverity.Warning,
                    $"Invalid FK type for '{fieldName}' at row {row.RowIndex} for entity '{rawEntitySet.EntityName}'.",
                    rawEntitySet.EntityName,
                    $"$[{row.RowIndex}].{fieldName}"));
                continue;
            }

            if (!_canonicalizer.TryCanonicalize(fkElement, out var fkKey, out _))
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidFkType,
                    WarningSeverity.Warning,
                    $"Invalid FK type for '{fieldName}' at row {row.RowIndex} for entity '{rawEntitySet.EntityName}'.",
                    rawEntitySet.EntityName,
                    $"$[{row.RowIndex}].{fieldName}"));
                continue;
            }

            if (!rowsByKey.TryGetValue(fkKey, out var list))
            {
                list = new List<RawEntityRow>();
                rowsByKey[fkKey] = list;
            }

            list.Add(row);
        }

        var readOnly = rowsByKey.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<RawEntityRow>)pair.Value,
            rowsByKey.Comparer);

        return new FkIndex(parentEntityName, rawEntitySet.EntityName, fieldName, readOnly);
    }
}
