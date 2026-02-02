using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputWriter
{
    public Task WriteAsync(
        string outputRoot,
        CompilationResult compilationResult,
        IndexBuildResult indexBuildResult,
        OutputLayoutPolicy layoutPolicy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Output root must be provided.", nameof(outputRoot));
        }

        var documentsByEntity = GroupDocuments(compilationResult.Documents);
        foreach (var entityName in documentsByEntity.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = layoutPolicy.GetEntityFolderName(entityName);
            var documentsPath = layoutPolicy.GetCompiledDocumentsPath(outputRoot, folder);
            var payloadArray = new JsonArray();
            foreach (var document in documentsByEntity[entityName])
            {
                payloadArray.Add(document.Payload);
            }

            WriteJsonAtomic(documentsPath, payloadArray, cancellationToken);
        }

        foreach (var pair in indexBuildResult.PkIndexes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entityName = pair.Key;
            var folder = layoutPolicy.GetEntityFolderName(entityName);
            var indexPath = layoutPolicy.GetPkIndexPath(outputRoot, folder);

            var pkMap = new JsonObject();
            if (documentsByEntity.TryGetValue(entityName, out var docs))
            {
                for (var i = 0; i < docs.Count; i++)
                {
                    var key = docs[i].PrimaryKey.CanonicalValue;
                    pkMap[key] = i;
                }
            }

            WriteJsonAtomic(indexPath, pkMap, cancellationToken);
        }

        var childPkLookup = BuildPkLookup(indexBuildResult.PkIndexes);

        foreach (var fkIndex in indexBuildResult.FkIndexes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = layoutPolicy.GetEntityFolderName(fkIndex.ParentEntityName);
            var indexPath = layoutPolicy.GetFkIndexPath(outputRoot, folder, fkIndex.EntityName, fkIndex.FieldName);

            var fkMap = new JsonObject();
            foreach (var pair in fkIndex.RowsByKey)
            {
                var array = new JsonArray();
                if (childPkLookup.TryGetValue(fkIndex.EntityName, out var pkByRow))
                {
                    foreach (var row in pair.Value)
                    {
                        if (pkByRow.TryGetValue(row, out var childKey))
                        {
                            array.Add(childKey.CanonicalValue);
                        }
                    }
                }

                fkMap[pair.Key.CanonicalValue] = array;
            }

            WriteJsonAtomic(indexPath, fkMap, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, List<CompiledDocument>> GroupDocuments(IReadOnlyList<CompiledDocument> documents)
    {
        var result = new Dictionary<string, List<CompiledDocument>>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            if (!result.TryGetValue(document.EntityName, out var list))
            {
                list = new List<CompiledDocument>();
                result[document.EntityName] = list;
            }

            list.Add(document);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, Dictionary<RawEntityRow, KeyValue>> BuildPkLookup(
        IReadOnlyDictionary<string, PkIndex> pkIndexes)
    {
        var result = new Dictionary<string, Dictionary<RawEntityRow, KeyValue>>(StringComparer.Ordinal);
        foreach (var pair in pkIndexes)
        {
            var map = new Dictionary<RawEntityRow, KeyValue>();
            foreach (var entry in pair.Value.RowsByKey)
            {
                map[entry.Value] = entry.Key;
            }

            result[pair.Key] = map;
        }

        return result;
    }

    private static void WriteJsonAtomic(string path, JsonNode node, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = false };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(node, options);

        var tempPath = $"{path}.tmp";
        File.WriteAllBytes(tempPath, bytes);
        cancellationToken.ThrowIfCancellationRequested();

        File.Move(tempPath, path, true);
    }
}
