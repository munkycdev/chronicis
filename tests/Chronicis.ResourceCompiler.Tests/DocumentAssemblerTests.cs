using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Manifest;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Raw;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

[ExcludeFromCodeCoverage]
public sealed class DocumentAssemblerTests
{
    [Fact]
    public async Task AssemblesParentChildWithOrdering()
    {
        var manifest = await LoadManifest("manifests", "phase4", "assembly-basic.yml");
        var raw = await LoadRaw(manifest);
        var indexes = BuildIndexes(manifest, raw);
        var assembler = new DocumentAssembler();

        var result = await assembler.AssembleAsync(manifest, raw, indexes, new CompilerOptions { MaxDepth = 3 }, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Documents);

        var payload = Assert.IsType<JsonObject>(result.Documents[0].Payload);
        var children = Assert.IsType<JsonArray>(payload["children"]);
        Assert.Equal(2, children.Count);

        var first = Assert.IsType<JsonObject>(children[0]);
        var second = Assert.IsType<JsonObject>(children[1]);
        Assert.Equal(1, first["order"]?.GetValue<int>());
        Assert.Equal(2, second["order"]?.GetValue<int>());
    }

    [Fact]
    public async Task MissingOrderFieldWarnsAndSortsLast()
    {
        var manifest = await LoadManifest("manifests", "phase4", "order-missing.yml");
        var raw = await LoadRaw(manifest);
        var indexes = BuildIndexes(manifest, raw);
        var assembler = new DocumentAssembler();

        var result = await assembler.AssembleAsync(manifest, raw, indexes, new CompilerOptions { MaxDepth = 3 }, CancellationToken.None);

        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.OrderByFieldMissing && warning.Severity == WarningSeverity.Warning);

        var payload = Assert.IsType<JsonObject>(result.Documents[0].Payload);
        var children = Assert.IsType<JsonArray>(payload["children"]);
        var last = Assert.IsType<JsonObject>(children[^1]);
        Assert.Equal("MissingOrder", last["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task CycleDetectionStopsDescent()
    {
        var manifest = await LoadManifest("manifests", "phase4", "cycle.yml");
        var raw = await LoadRaw(manifest);
        var indexes = BuildIndexes(manifest, raw);
        var assembler = new DocumentAssembler();

        var result = await assembler.AssembleAsync(manifest, raw, indexes, new CompilerOptions { MaxDepth = 3 }, CancellationToken.None);

        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.CycleDetected && warning.Severity == WarningSeverity.Warning);

        var payload = Assert.IsType<JsonObject>(result.Documents[0].Payload);
        var children = Assert.IsType<JsonArray>(payload["bs"]);
        var childPayload = Assert.IsType<JsonObject>(children[0]);
        var cycleChildren = Assert.IsType<JsonArray>(childPayload["as"]);
        Assert.Empty(cycleChildren);
    }

    [Fact]
    public async Task MaxDepthStopsDescent()
    {
        var manifest = await LoadManifest("manifests", "phase4", "max-depth.yml");
        var raw = await LoadRaw(manifest);
        var indexes = BuildIndexes(manifest, raw);
        var assembler = new DocumentAssembler();

        var result = await assembler.AssembleAsync(manifest, raw, indexes, new CompilerOptions { MaxDepth = 1 }, CancellationToken.None);

        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.MaxDepthExceeded && warning.Severity == WarningSeverity.Warning);

        var payload = Assert.IsType<JsonObject>(result.Documents[0].Payload);
        var children = Assert.IsType<JsonArray>(payload["children"]);
        var childPayload = Assert.IsType<JsonObject>(children[0]);
        var grandChildren = Assert.IsType<JsonArray>(childPayload["grandchildren"]);
        Assert.Empty(grandChildren);
    }

    [Fact]
    public async Task AssemblyIsDeterministic()
    {
        var manifest = await LoadManifest("manifests", "phase4", "assembly-basic.yml");
        var raw = await LoadRaw(manifest);
        var indexes = BuildIndexes(manifest, raw);
        var assembler = new DocumentAssembler();
        var options = new CompilerOptions { MaxDepth = 3 };

        var result1 = await assembler.AssembleAsync(manifest, raw, indexes, options, CancellationToken.None);
        var result2 = await assembler.AssembleAsync(manifest, raw, indexes, options, CancellationToken.None);

        var json1 = result1.Documents[0].Payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var json2 = result2.Documents[0].Payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        Assert.Equal(json1, json2);
    }

    private static IndexBuildResult BuildIndexes(Manifest.Models.Manifest manifest, RawLoadResult raw)
    {
        var builder = new IndexBuilder();
        return builder.BuildIndexes(manifest, raw);
    }

    private static async Task<Manifest.Models.Manifest> LoadManifest(params string[] segments)
    {
        var loader = new ManifestLoader();
        var validator = new ManifestValidator();
        var path = GetTestDataPath(segments);

        var result = await loader.LoadAsync(path, CancellationToken.None);
        Assert.NotNull(result.Manifest);

        var warnings = validator.Validate(result.Manifest!);
        Assert.DoesNotContain(warnings, warning => warning.Severity == WarningSeverity.Error);
        return result.Manifest!;
    }

    private static async Task<RawLoadResult> LoadRaw(Manifest.Models.Manifest manifest)
    {
        var baseDir = GetTestDataPath("raw", "phase4");
        var loader = new RawDataLoader();
        return await loader.LoadAsync(manifest, baseDir, CancellationToken.None);
    }

    private static string GetTestDataPath(params string[] segments)
    {
        var pathSegments = new List<string> { AppContext.BaseDirectory, "..", "..", "..", "TestData" };
        pathSegments.AddRange(segments);
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }
}
