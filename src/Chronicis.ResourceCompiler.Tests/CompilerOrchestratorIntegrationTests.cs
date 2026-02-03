using System.Linq;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Manifest;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

public sealed class CompilerOrchestratorIntegrationTests
{
    [Fact]
    public async Task OrchestratorRunsEndToEndAndWritesOutputs()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var options = new CompilerOptions
            {
                ManifestPath = GetTestDataPath("manifests", "phase7", "happy.yml"),
                RawPath = GetTestDataPath("raw", "phase7"),
                OutputRoot = outputRoot,
                MaxDepth = 3
            };

            var orchestrator = new CompilerOrchestrator();
            var result = await orchestrator.RunAsync(options, CancellationToken.None);

            Assert.False(result.HasErrors);

            var childName = await ResolveChildName(options.ManifestPath, "Parent");
            var firstPath = Path.Combine(outputRoot, "parents", "parent-one.json");
            var secondPath = Path.Combine(outputRoot, "parents", "parent-two.json");
            Assert.True(File.Exists(firstPath));
            Assert.True(File.Exists(secondPath));

            var firstJson = JsonNode.Parse(File.ReadAllText(firstPath)) as JsonObject;
            Assert.NotNull(firstJson);
            var children = Assert.IsType<JsonArray>(firstJson![childName]);
            Assert.Equal(2, children.Count);

            var indexPath = Path.Combine(outputRoot, "indexes", "parents.json");
            Assert.True(File.Exists(indexPath));
            var indexArray = JsonNode.Parse(File.ReadAllText(indexPath)) as JsonArray;
            Assert.NotNull(indexArray);
            Assert.Equal(2, indexArray!.Count);

            Assert.Empty(Directory.GetFiles(outputRoot, "documents.json", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetFiles(outputRoot, "by-pk.json", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetDirectories(outputRoot, "fk", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OrchestratorStopsOnOutputCollision()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var options = new CompilerOptions
            {
                ManifestPath = GetTestDataPath("manifests", "phase7", "collision.yml"),
                RawPath = GetTestDataPath("raw", "phase7"),
                OutputRoot = outputRoot,
                MaxDepth = 3
            };

            var orchestrator = new CompilerOrchestrator();
            var result = await orchestrator.RunAsync(options, CancellationToken.None);

            Assert.True(result.HasErrors);
            Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.OutputBlobPathCollision && warning.Severity == WarningSeverity.Error);
            Assert.False(Directory.Exists(outputRoot));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string GetTestDataPath(params string[] segments)
    {
        var pathSegments = new List<string> { AppContext.BaseDirectory, "..", "..", "..", "TestData" };
        pathSegments.AddRange(segments);
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"chronicis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static async Task<string> ResolveChildName(string manifestPath, string parentEntity)
    {
        var loader = new ManifestLoader();
        var result = await loader.LoadAsync(manifestPath, CancellationToken.None);
        Assert.NotNull(result.Manifest);

        var parent = result.Manifest!.Entities[parentEntity];
        var child = parent.Children.First();
        var resolved = string.IsNullOrWhiteSpace(child.As) ? child.Entity : child.As;
        Assert.False(string.IsNullOrWhiteSpace(resolved));
        return resolved;
    }
}
