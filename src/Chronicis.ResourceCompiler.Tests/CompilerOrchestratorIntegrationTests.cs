using System.Linq;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Manifest;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Output;
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
                ManifestPath = GetTestDataPath("manifests", "phase4", "assembly-basic.yml"),
                RawPath = GetTestDataPath("raw", "phase4"),
                OutputRoot = outputRoot,
                MaxDepth = 3
            };

            var orchestrator = new CompilerOrchestrator();
            var result = await orchestrator.RunAsync(options, CancellationToken.None);

            Assert.False(result.HasErrors);

            var childName = await ResolveChildName(options.ManifestPath, "Parent");
            var layout = new OutputLayoutPolicy();
            var parentFolder = layout.GetEntityFolderName("Parent");
            var childFolder = layout.GetEntityFolderName("Child");

            var documentsPath = layout.GetCompiledDocumentsPath(outputRoot, parentFolder);
            Assert.True(File.Exists(documentsPath));

            var documentsJson = JsonNode.Parse(File.ReadAllText(documentsPath)) as JsonArray;
            Assert.NotNull(documentsJson);
            var first = Assert.IsType<JsonObject>(documentsJson![0]);
            var children = Assert.IsType<JsonArray>(first[childName]);
            Assert.Equal(2, children.Count);

            var pkPath = layout.GetPkIndexPath(outputRoot, parentFolder);
            var pkJson = JsonNode.Parse(File.ReadAllText(pkPath)) as JsonObject;
            Assert.NotNull(pkJson);
            Assert.Equal(0, pkJson!["1"]!.GetValue<int>());
            Assert.Single(pkJson);

            var fkPath = layout.GetFkIndexPath(outputRoot, parentFolder, "Child", "parentId");
            Assert.True(File.Exists(fkPath));
            var fkJson = JsonNode.Parse(File.ReadAllText(fkPath)) as JsonObject;
            Assert.NotNull(fkJson);
            var fkArray = fkJson!["1"] as JsonArray;
            Assert.NotNull(fkArray);
            Assert.Equal(2, fkArray!.Count);

            var childPkPath = layout.GetPkIndexPath(outputRoot, childFolder);
            Assert.True(File.Exists(childPkPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task OrchestratorStopsOnMissingRawFile()
    {
        var tempDir = CreateTempDir();
        try
        {
            var outputRoot = Path.Combine(tempDir, "out");
            var options = new CompilerOptions
            {
                ManifestPath = GetTestDataPath("manifests", "phase6", "missing-raw.yml"),
                RawPath = GetTestDataPath("raw", "phase4"),
                OutputRoot = outputRoot,
                MaxDepth = 3
            };

            var orchestrator = new CompilerOrchestrator();
            var result = await orchestrator.RunAsync(options, CancellationToken.None);

            Assert.True(result.HasErrors);
            Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.RawFileNotFound && warning.Severity == WarningSeverity.Error);
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
