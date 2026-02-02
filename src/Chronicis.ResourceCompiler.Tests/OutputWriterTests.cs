using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Output;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

public sealed class OutputWriterTests
{
    [Fact]
    public async Task WritesDocumentsAndIndexesToExpectedPaths()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var layout = new OutputLayoutPolicy();

            var documents = new List<CompiledDocument>
            {
                new("Parent", new KeyValue(KeyKind.Number, "1"), new JsonObject
                {
                    ["id"] = 1,
                    ["name"] = "First"
                }),
                new("Parent", new KeyValue(KeyKind.Number, "2"), new JsonObject
                {
                    ["id"] = 2,
                    ["name"] = "Second"
                })
            };

            var compilation = new CompilationResult(documents, Array.Empty<Warning>());

            var pkIndexes = new Dictionary<string, PkIndex>
            {
                ["Parent"] = new PkIndex("Parent", new Dictionary<KeyValue, RawEntityRow>())
            };

            var childRow1 = CreateRawRow("Child", 0, "{ \"id\": 10, \"parentId\": 1 }");
            var childRow2 = CreateRawRow("Child", 1, "{ \"id\": 11, \"parentId\": 1 }");

            var fkRows = new Dictionary<KeyValue, IReadOnlyList<RawEntityRow>>
            {
                [new KeyValue(KeyKind.Number, "1")] = new List<RawEntityRow>
                {
                    childRow1,
                    childRow2
                }
            };

            var fkIndexes = new List<FkIndex>
            {
                new("Parent", "Child", "parentId", fkRows)
            };

            var childPkIndex = new PkIndex("Child", new Dictionary<KeyValue, RawEntityRow>
            {
                [new KeyValue(KeyKind.Number, "10")] = childRow1,
                [new KeyValue(KeyKind.Number, "11")] = childRow2
            });

            var indexResult = new IndexBuildResult(
                new Dictionary<string, PkIndex>
                {
                    ["Parent"] = pkIndexes["Parent"],
                    ["Child"] = childPkIndex
                },
                fkIndexes,
                Array.Empty<Warning>());
            var writer = new OutputWriter();

            await writer.WriteAsync(outputRoot, compilation, indexResult, layout, CancellationToken.None);

            var entityFolder = layout.GetEntityFolderName("Parent");
            var documentsPath = layout.GetCompiledDocumentsPath(outputRoot, entityFolder);
            Assert.True(File.Exists(documentsPath));

            var documentsJson = JsonNode.Parse(File.ReadAllText(documentsPath)) as JsonArray;
            Assert.NotNull(documentsJson);
            Assert.Equal(2, documentsJson!.Count);

            var pkPath = layout.GetPkIndexPath(outputRoot, entityFolder);
            var pkJson = JsonNode.Parse(File.ReadAllText(pkPath)) as JsonObject;
            Assert.NotNull(pkJson);
            Assert.Equal(0, pkJson!["1"]!.GetValue<int>());
            Assert.Equal(1, pkJson!["2"]!.GetValue<int>());

            var fkPath = layout.GetFkIndexPath(outputRoot, entityFolder, "Child", "parentId");
            var fkJson = JsonNode.Parse(File.ReadAllText(fkPath)) as JsonObject;
            Assert.NotNull(fkJson);
            var fkArray = fkJson!["1"] as JsonArray;
            Assert.NotNull(fkArray);
            Assert.Equal(2, fkArray!.Count);
            Assert.Equal("10", fkArray[0]!.GetValue<string>());
            Assert.Equal("11", fkArray[1]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static RawEntityRow CreateRawRow(string entityName, int rowIndex, string json)
    {
        using var document = JsonDocument.Parse(json);
        var element = document.RootElement.Clone();
        return new RawEntityRow(entityName, rowIndex, element, element);
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"chronicis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
