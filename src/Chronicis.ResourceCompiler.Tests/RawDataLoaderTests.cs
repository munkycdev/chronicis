using System.Text.Json;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

public sealed class RawDataLoaderTests
{
    [Fact]
    public async Task LoadsValidArrayAndExtractsPk()
    {
        var result = await Load("valid.json");

        Assert.False(result.HasErrors);
        Assert.Single(result.EntitySets);

        var set = result.EntitySets[0];
        Assert.Equal("TestEntity", set.EntityName);
        Assert.Equal(2, set.Rows.Count);
        Assert.All(set.Rows, row => Assert.True(row.ExtractedPkElement.HasValue));
        Assert.All(set.Rows, row => Assert.Equal(JsonValueKind.Number, row.ExtractedPkElement!.Value.ValueKind));
    }

    [Fact]
    public async Task EmitsErrorWhenRootNotArray()
    {
        var result = await Load("root-not-array.json");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.RawRootNotArray && warning.Severity == WarningSeverity.Error);
        Assert.Single(result.EntitySets);
        Assert.Empty(result.EntitySets[0].Rows);
    }

    [Fact]
    public async Task EmitsErrorWhenRowNotObject()
    {
        var result = await Load("row-not-object.json");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.RawRowNotObject && warning.Severity == WarningSeverity.Error);
        Assert.Single(result.EntitySets);
        Assert.Single(result.EntitySets[0].Rows);
    }

    [Fact]
    public async Task EmitsErrorWhenMissingPk()
    {
        var result = await Load("missing-pk.json");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.MissingPk && warning.Severity == WarningSeverity.Error);
        Assert.Single(result.EntitySets);
        Assert.Single(result.EntitySets[0].Rows);
        Assert.False(result.EntitySets[0].Rows[0].ExtractedPkElement.HasValue);
    }

    [Fact]
    public async Task EmitsErrorWhenInvalidPkType()
    {
        var result = await Load("invalid-pk.json");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.InvalidPkType && warning.Severity == WarningSeverity.Error);
        Assert.Single(result.EntitySets);
        Assert.Equal(3, result.EntitySets[0].Rows.Count);
        Assert.All(result.EntitySets[0].Rows, row => Assert.True(row.ExtractedPkElement.HasValue));
    }

    private static async Task<Raw.Models.RawLoadResult> Load(string fileName)
    {
        var loader = new RawDataLoader();
        var manifest = new Manifest.Models.Manifest
        {
            Entities = new Dictionary<string, ManifestEntity>
            {
                ["TestEntity"] = new ManifestEntity
                {
                    Name = "TestEntity",
                    File = fileName,
                    PrimaryKey = "id"
                }
            }
        };

        var baseDir = GetTestDataPath("raw", "phase2");
        return await loader.LoadAsync(manifest, baseDir, CancellationToken.None);
    }

    private static string GetTestDataPath(params string[] segments)
    {
        var pathSegments = new List<string> { AppContext.BaseDirectory, "..", "..", "..", "TestData" };
        pathSegments.AddRange(segments);
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }
}
