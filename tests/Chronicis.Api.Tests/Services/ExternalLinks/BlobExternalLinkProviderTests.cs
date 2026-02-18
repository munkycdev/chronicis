using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Api.Services.ExternalLinks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class BlobExternalLinkProviderTests
{
    private static BlobExternalLinkProviderOptions CreateOptions() => new()
    {
        Key = "srd14",
        DisplayName = "SRD 2014",
        ConnectionString = "UseDevelopmentStorage=true",
        ContainerName = "external-links",
        RootPrefix = "2014/"
    };

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new BlobExternalLinkProvider(null!, cache, NullLogger<BlobExternalLinkProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullCache_Throws()
    {
        var options = CreateOptions();

        Assert.Throws<ArgumentNullException>(() =>
            new BlobExternalLinkProvider(options, null!, NullLogger<BlobExternalLinkProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new BlobExternalLinkProvider(options, cache, null!));
    }

    [Fact]
    public void Key_ReturnsConfiguredProviderKey()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        Assert.Equal("srd14", sut.Key);
    }

    [Fact]
    public async Task GetContentAsync_InvalidId_ReturnsNotFoundContentWithoutStorageCall()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        var result = await sut.GetContentAsync("../etc/passwd", CancellationToken.None);

        Assert.Equal("srd14", result.Source);
        Assert.Equal("../etc/passwd", result.Id);
        Assert.Equal("Content Not Found", result.Title);
        Assert.Equal("Unknown", result.Kind);
        Assert.Equal("The requested content could not be found.", result.Markdown);
        Assert.Equal("Source: SRD 2014", result.Attribution);
        Assert.Null(result.ExternalUrl);
    }

    [Fact]
    public void BuildCacheKey_WithAndWithoutKey_ReturnsExpectedValue()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        var method = typeof(BlobExternalLinkProvider).GetMethod(
            "BuildCacheKey",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var withoutKey = (string)method.Invoke(sut, ["Children", null])!;
        var withKey = (string)method.Invoke(sut, ["Children", "path-a"])!;

        Assert.Equal("ExternalLinks:srd14:Children", withoutKey);
        Assert.Equal("ExternalLinks:srd14:Children:path-a", withKey);
    }

    [Fact]
    public void FilterChildFiles_EmptySearchTerm_ReturnsAllFiles()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        var method = typeof(BlobExternalLinkProvider).GetMethod(
            "FilterChildFiles",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var files = new List<CategoryItem>
        {
            new("spells/fireball", "Fireball", "2014/spells/fireball.json", null),
            new("spells/acid-arrow", "Acid Arrow", "2014/spells/acid-arrow.json", null)
        };

        var result = (IEnumerable<ExternalLinkSuggestion>)method.Invoke(
            sut,
            [files, "", "spells"])!;

        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, x =>
        {
            Assert.Equal("srd14", x.Source);
            Assert.Equal("Spells", x.Subtitle);
            Assert.Equal("spells", x.Category);
        });
    }

    [Fact]
    public void FilterChildFiles_MultiTokenSearch_UsesAndMatching()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        var method = typeof(BlobExternalLinkProvider).GetMethod(
            "FilterChildFiles",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var files = new List<CategoryItem>
        {
            new("spells/acid-arrow", "Acid Arrow", "2014/spells/acid-arrow.json", null),
            new("spells/arrow-storm", "Arrow Storm", "2014/spells/arrow-storm.json", null),
            new("spells/fireball", "Fireball", "2014/spells/fireball.json", null)
        };

        var result = (IEnumerable<ExternalLinkSuggestion>)method.Invoke(
            sut,
            [files, "acid arrow", "spells"])!;

        var only = Assert.Single(result);
        Assert.Equal("Acid Arrow", only.Title);
        Assert.Equal("spells/acid-arrow", only.Id);
    }

    [Fact]
    public void CacheBlobPathMapping_WritesExpectedCacheEntry()
    {
        var options = CreateOptions();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new BlobExternalLinkProvider(options, cache, NullLogger<BlobExternalLinkProvider>.Instance);

        var method = typeof(BlobExternalLinkProvider).GetMethod(
            "CacheBlobPathMapping",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(sut, ["bestiary/beast", "Bestiary/Beast"]);

        Assert.True(cache.TryGetValue("ExternalLinks:srd14:BlobPathMap:bestiary/beast", out string? mapped));
        Assert.Equal("Bestiary/Beast", mapped);
    }
}
