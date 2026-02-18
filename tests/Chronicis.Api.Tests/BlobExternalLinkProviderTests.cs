using System.Diagnostics.CodeAnalysis;
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
}
