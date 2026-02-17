using System.Diagnostics.CodeAnalysis;
using Chronicis.ResourceCompiler.Manifest;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

[ExcludeFromCodeCoverage]
public sealed class ScaffoldTests
{
    [Fact]
    public async Task LoadValidManifest()
    {
        var loader = new ManifestLoader();
        var manifestPath = GetTestDataPath("srd-manifest.yml");

        var result = await loader.LoadAsync(manifestPath, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.NotNull(result.Manifest);

        var manifest = result.Manifest!;
        Assert.Equal(24, manifest.Entities.Count);
        Assert.True(manifest.Entities.ContainsKey("Creature"));
        Assert.True(manifest.Entities.ContainsKey("Spell"));

        var creature = manifest.Entities["Creature"];
        Assert.True(creature.IsRoot);

        var creatureAction = manifest.Entities["CreatureAction"];
        Assert.False(creatureAction.IsRoot);

        var actionChild = creature.Children.Single(child => child.Entity == "CreatureAction");
        Assert.Equal("creature", actionChild.ForeignKeyField);
        Assert.NotNull(actionChild.OrderBy);
        Assert.Equal("order", actionChild.OrderBy!.Field);
        Assert.Equal(ManifestOrderByDirection.Asc, actionChild.OrderBy.Direction);
    }

    [Fact]
    public async Task InvalidManifestEmitsErrors()
    {
        var loader = new ManifestLoader();
        var validator = new ManifestValidator();
        var manifestPath = GetTestDataPath("manifests", "invalid-manifest.yml");

        var result = await loader.LoadAsync(manifestPath, CancellationToken.None);

        Assert.NotNull(result.Manifest);

        var warnings = validator.Validate(result.Manifest!);
        Assert.NotEmpty(warnings);
        Assert.Contains(warnings, warning => warning.Code == WarningCode.MissingKey && warning.Severity == WarningSeverity.Error);
        Assert.Contains(warnings, warning => warning.Code == WarningCode.MissingForeignKey && warning.Severity == WarningSeverity.Error);
        Assert.Contains(warnings, warning => warning.Code == WarningCode.OrderByFieldMissing && warning.Severity == WarningSeverity.Error);
        Assert.Contains(warnings, warning => warning.Code == WarningCode.InvalidManifest && warning.Severity == WarningSeverity.Error);
    }

    private static string GetTestDataPath(params string[] segments)
    {
        var pathSegments = new List<string> { AppContext.BaseDirectory, "..", "..", "..", "TestData" };
        pathSegments.AddRange(segments);
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }
}
