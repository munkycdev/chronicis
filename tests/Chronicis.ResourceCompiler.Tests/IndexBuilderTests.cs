using System.Diagnostics.CodeAnalysis;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

[ExcludeFromCodeCoverage]
public sealed class IndexBuilderTests
{
    [Fact]
    public async Task BuildsPkIndexForUniqueKeys()
    {
        var raw = await LoadRaw("pk-valid.json");
        var manifest = CreateManifest(("Parent", "pk-valid.json"));
        var builder = new IndexBuilder();

        var result = builder.BuildIndexes(manifest, raw);

        Assert.False(result.HasErrors);
        Assert.True(result.PkIndexes.ContainsKey("Parent"));
        var rows = result.PkIndexes["Parent"].RowsByKey.Values
            .OrderBy(row => row.RowIndex)
            .ToArray();
        Assert.Collection(rows, _ => { }, _ => { });
    }

    [Fact]
    public async Task DuplicatePkEmitsErrorAndKeepsFirst()
    {
        var raw = await LoadRaw("pk-duplicate.json");
        var manifest = CreateManifest(("Parent", "pk-duplicate.json"));
        var builder = new IndexBuilder();

        var result = builder.BuildIndexes(manifest, raw);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.DuplicatePk && warning.Severity == WarningSeverity.Error);
        Assert.Single(result.PkIndexes["Parent"].RowsByKey);
        Assert.Equal(0, result.PkIndexes["Parent"].RowsByKey.Values.First().RowIndex);
    }

    [Fact]
    public async Task MissingPkEmitsError()
    {
        var raw = await LoadRaw("pk-missing.json");
        var manifest = CreateManifest(("Parent", "pk-missing.json"));
        var builder = new IndexBuilder();

        var result = builder.BuildIndexes(manifest, raw);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.MissingPk && warning.Severity == WarningSeverity.Error);
        Assert.Empty(result.PkIndexes["Parent"].RowsByKey);
    }

    [Fact]
    public async Task InvalidPkTypeEmitsError()
    {
        var raw = await LoadRaw("pk-invalid.json");
        var manifest = CreateManifest(("Parent", "pk-invalid.json"));
        var builder = new IndexBuilder();

        var result = builder.BuildIndexes(manifest, raw);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.InvalidPkType && warning.Severity == WarningSeverity.Error);
        Assert.Empty(result.PkIndexes["Parent"].RowsByKey);
    }

    [Fact]
    public async Task BuildsFkIndexAndEmitsWarnings()
    {
        var raw = await LoadRaw("fk-parent.json", "fk-child.json");
        var manifest = new Manifest.Models.Manifest
        {
            Entities = new Dictionary<string, ManifestEntity>
            {
                ["Parent"] = new ManifestEntity
                {
                    Name = "Parent",
                    File = "fk-parent.json",
                    PrimaryKey = "id",
                    Children = new[]
                    {
                        new ManifestChild
                        {
                            Entity = "Child",
                            ForeignKeyField = "parentId"
                        }
                    }
                },
                ["Child"] = new ManifestEntity
                {
                    Name = "Child",
                    File = "fk-child.json",
                    PrimaryKey = "id"
                }
            }
        };

        var builder = new IndexBuilder();
        var result = builder.BuildIndexes(manifest, raw);

        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.MissingFk && warning.Severity == WarningSeverity.Warning);
        Assert.Contains(result.Warnings, warning => warning.Code == WarningCode.InvalidFkType && warning.Severity == WarningSeverity.Warning);

        Assert.Single(result.FkIndexes);
        var fkIndex = result.FkIndexes[0];
        Assert.Equal("Parent", fkIndex.ParentEntityName);
        Assert.Equal("Child", fkIndex.EntityName);
        Assert.Equal("parentId", fkIndex.FieldName);
        Assert.True(fkIndex.RowsByKey.Any());
        Assert.Equal(2, fkIndex.RowsByKey.Values.First().Count);
    }

    [Fact]
    public async Task BuildsFkIndexWithNestedField()
    {
        var manifest = new Manifest.Models.Manifest
        {
            Entities = new Dictionary<string, ManifestEntity>
            {
                ["Parent"] = new ManifestEntity
                {
                    Name = "Parent",
                    File = "fk-nested-parent.json",
                    PrimaryKey = "id",
                    Children = new[]
                    {
                        new ManifestChild
                        {
                            Entity = "Child",
                            ForeignKeyField = "fields.parentId"
                        }
                    }
                },
                ["Child"] = new ManifestEntity
                {
                    Name = "Child",
                    File = "fk-nested-child.json",
                    PrimaryKey = "id"
                }
            }
        };

        var baseDir = GetTestDataPath("raw", "phase3");
        var loader = new RawDataLoader();
        var raw = await loader.LoadAsync(manifest, baseDir, CancellationToken.None);
        var builder = new IndexBuilder();

        var result = builder.BuildIndexes(manifest, raw);

        Assert.False(result.HasErrors);
        Assert.Single(result.FkIndexes);
        var fkIndex = result.FkIndexes[0];
        Assert.True(fkIndex.RowsByKey.TryGetValue(new Indexing.Models.KeyValue(Indexing.Models.KeyKind.Number, "1"), out var rows));
        Assert.Equal(2, rows.Count);
        Assert.DoesNotContain(result.Warnings, warning => warning.Code == WarningCode.MissingFk);
    }

    private static async Task<RawLoadResult> LoadRaw(params string[] fileNames)
    {
        var manifest = new Manifest.Models.Manifest
        {
            Entities = fileNames.ToDictionary(
                file => file == "fk-child.json" ? "Child" : "Parent",
                file => new ManifestEntity
                {
                    Name = file == "fk-child.json" ? "Child" : "Parent",
                    File = file,
                    PrimaryKey = "id"
                })
        };

        var baseDir = GetTestDataPath("raw", "phase3");
        var loader = new RawDataLoader();
        return await loader.LoadAsync(manifest, baseDir, CancellationToken.None);
    }

    private static Manifest.Models.Manifest CreateManifest(params (string EntityName, string FileName)[] entities)
    {
        return new Manifest.Models.Manifest
        {
            Entities = entities.ToDictionary(
                entry => entry.EntityName,
                entry => new ManifestEntity
                {
                    Name = entry.EntityName,
                    File = entry.FileName,
                    PrimaryKey = "id"
                })
        };
    }

    private static string GetTestDataPath(params string[] segments)
    {
        var pathSegments = new List<string> { AppContext.BaseDirectory, "..", "..", "..", "TestData" };
        pathSegments.AddRange(segments);
        return Path.GetFullPath(Path.Combine(pathSegments.ToArray()));
    }
}
