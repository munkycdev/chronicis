using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Output;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

public sealed class OutputWriterTests
{
    [Fact]
    public async Task WritesManifestDefinedOutputs()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var manifest = new Manifest.Models.Manifest
            {
                Entities = new Dictionary<string, ManifestEntity>
                {
                    ["Parent"] = new ManifestEntity
                    {
                        Name = "Parent",
                        IsRoot = true,
                        Output = new ManifestOutput
                        {
                            BlobTemplate = "parents/{slug}.json",
                            Index = new ManifestOutputIndex
                            {
                                Blob = "indexes/parents.json",
                                Fields = new[] { "id", "slug", "name", "missing" }
                            }
                        },
                        Identity = new ManifestIdentity
                        {
                            SlugField = "slug"
                        }
                    }
                }
            };

            var documents = new List<CompiledDocument>
            {
                new("Parent", new KeyValue(KeyKind.Number, "1"), new JsonObject
                {
                    ["id"] = 1,
                    ["slug"] = "parent-one",
                    ["name"] = "First"
                }),
                new("Parent", new KeyValue(KeyKind.Number, "2"), new JsonObject
                {
                    ["id"] = 2,
                    ["slug"] = "parent-two",
                    ["name"] = "Second"
                })
            };

            var compilation = new CompilationResult(documents, Array.Empty<Warning>());
            var writer = new OutputWriter();

            var result = await writer.WriteAsync(outputRoot, manifest, compilation, CancellationToken.None);

            Assert.False(result.HasErrors);
            Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.OutputIndexFieldMissing);

            var firstPath = Path.Combine(outputRoot, "parents", "parent-one.json");
            var secondPath = Path.Combine(outputRoot, "parents", "parent-two.json");
            Assert.True(File.Exists(firstPath));
            Assert.True(File.Exists(secondPath));

            var indexPath = Path.Combine(outputRoot, "indexes", "parents.json");
            Assert.True(File.Exists(indexPath));
            var indexArray = JsonNode.Parse(File.ReadAllText(indexPath)) as JsonArray;
            Assert.NotNull(indexArray);
            Assert.Equal(2, indexArray!.Count);

            var indexFirst = Assert.IsType<JsonObject>(indexArray[0]);
            Assert.Equal(1, indexFirst["id"]!.GetValue<int>());
            Assert.Equal("parent-one", indexFirst["slug"]!.GetValue<string>());
            Assert.Equal("First", indexFirst["name"]!.GetValue<string>());
            Assert.True(indexFirst.ContainsKey("missing"));
            Assert.Null(indexFirst["missing"]);

            Assert.Empty(Directory.GetFiles(outputRoot, "documents.json", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetFiles(outputRoot, "by-pk.json", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task MissingSlugTokenEmitsError()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var manifest = new Manifest.Models.Manifest
            {
                Entities = new Dictionary<string, ManifestEntity>
                {
                    ["Parent"] = new ManifestEntity
                    {
                        Name = "Parent",
                        IsRoot = true,
                        Output = new ManifestOutput
                        {
                            BlobTemplate = "parents/{slug}.json"
                        },
                        Identity = new ManifestIdentity
                        {
                            SlugField = "slug"
                        }
                    }
                }
            };

            var documents = new List<CompiledDocument>
            {
                new("Parent", new KeyValue(KeyKind.Number, "1"), new JsonObject
                {
                    ["id"] = 1
                })
            };

            var compilation = new CompilationResult(documents, Array.Empty<Warning>());
            var writer = new OutputWriter();

            var result = await writer.WriteAsync(outputRoot, manifest, compilation, CancellationToken.None);

            Assert.True(result.HasErrors);
            Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.OutputTemplateMissingToken && warning.Severity == WarningSeverity.Error);
            Assert.Empty(Directory.GetFiles(outputRoot, "*.json", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"chronicis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir+"/out");
        return tempDir;
    }
}
