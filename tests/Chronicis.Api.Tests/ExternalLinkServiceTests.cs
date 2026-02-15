using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Repositories;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;


[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class ExternalLinkServiceTests : IDisposable
{
    private readonly IExternalLinkProviderRegistry _registry;
    private readonly IResourceProviderRepository _resourceProviderRepository;
    private readonly MemoryCache _cache;
    private readonly ExternalLinkService _sut;

    public ExternalLinkServiceTests()
    {
        _registry = Substitute.For<IExternalLinkProviderRegistry>();
        _resourceProviderRepository = Substitute.For<IResourceProviderRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExternalLinkService>.Instance;

        _sut = new ExternalLinkService(
            _registry,
            _resourceProviderRepository,
            _cache,
            logger);
    }

    private bool _disposed = false;
    public void Dispose()
    {

        Dispose(true);
        // Suppress finalization, as cleanup has been done.
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cache.Dispose();
        }

        _disposed = true;
    }

    // ── Cache key format ──────────────────────────────────────────

    [Fact]
    public void BuildSuggestionCacheKey_IsLowercaseAndCorrectlyFormatted()
    {
        var key = ExternalLinkService.BuildSuggestionCacheKey("SRD", "Fireball");

        Assert.Equal("external-links:suggestions:srd:fireball", key);
    }

    [Fact]
    public void BuildContentCacheKey_IsLowercaseAndCorrectlyFormatted()
    {
        var key = ExternalLinkService.BuildContentCacheKey("SRD", "/api/spells/fireball");

        Assert.Equal("external-links:content:srd:/api/spells/fireball", key);
    }

    // ── GetSuggestionsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetSuggestions_EmptySource_ReturnsEmpty()
    {
        var result = await _sut.GetSuggestionsAsync(null, "", "query", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestions_UnknownProvider_ReturnsEmpty()
    {
        _registry.GetProvider("unknown").Returns((IExternalLinkProvider?)null);

        var result = await _sut.GetSuggestionsAsync(null, "unknown", "test", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestions_ProviderThrows_ReturnsEmptyAndSwallowsException()
    {
        var provider = Substitute.For<IExternalLinkProvider>();
        provider.SearchAsync("test", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        _registry.GetProvider("srd").Returns(provider);

        var result = await _sut.GetSuggestionsAsync(null, "srd", "test", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestions_ValidProvider_ReturnsSuggestionsAndCaches()
    {
        var expected = new List<ExternalLinkSuggestion>
        {
            new() { Source = "srd", Id = "/api/spells/fireball", Title = "Fireball" }
        };

        var provider = Substitute.For<IExternalLinkProvider>();
        provider.SearchAsync("fire", Arg.Any<CancellationToken>()).Returns(expected);
        _registry.GetProvider("srd").Returns(provider);

        // First call — hits the provider
        var result1 = await _sut.GetSuggestionsAsync(null, "srd", "fire", CancellationToken.None);
        Assert.Single(result1);

        // Second call — should come from cache, provider not called again
        var result2 = await _sut.GetSuggestionsAsync(null, "srd", "fire", CancellationToken.None);
        Assert.Single(result2);

        await provider.Received(1).SearchAsync("fire", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSuggestions_WorldProviderDisabled_ReturnsEmpty()
    {
        var worldId = Guid.NewGuid();
        _resourceProviderRepository.GetWorldProvidersAsync(worldId)
            .Returns(
            [
                (new ResourceProvider { Code = "srd" }, false)
            ]);

        var result = await _sut.GetSuggestionsAsync(worldId, "srd", "test", CancellationToken.None);

        Assert.Empty(result);
        _registry.DidNotReceive().GetProvider(Arg.Any<string>());
    }

    [Fact]
    public async Task GetSuggestions_WorldProviderEnabled_ProceedsToProvider()
    {
        var worldId = Guid.NewGuid();
        _resourceProviderRepository.GetWorldProvidersAsync(worldId)
            .Returns(
            [
                (new ResourceProvider { Code = "srd" }, true)
            ]);

        var provider = Substitute.For<IExternalLinkProvider>();
        provider.SearchAsync("test", Arg.Any<CancellationToken>())
            .Returns([]);
        _registry.GetProvider("srd").Returns(provider);

        await _sut.GetSuggestionsAsync(worldId, "srd", "test", CancellationToken.None);

        await provider.Received(1).SearchAsync("test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSuggestions_NoWorldId_SkipsWorldProviderCheckAsync()
    {
        var provider = Substitute.For<IExternalLinkProvider>();
        provider.SearchAsync("test", Arg.Any<CancellationToken>())
            .Returns([]);
        _registry.GetProvider("srd").Returns(provider);

        await _sut.GetSuggestionsAsync(null, "srd", "test", CancellationToken.None);

        await _resourceProviderRepository.DidNotReceive()
            .GetWorldProvidersAsync(Arg.Any<Guid>());
    }

    // ── GetContentAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetContent_EmptySource_ReturnsNull()
    {
        var result = await _sut.GetContentAsync("", "some-id", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContent_EmptyId_ReturnsNull()
    {
        var result = await _sut.GetContentAsync("srd", "", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContent_UnknownProvider_ReturnsNull()
    {
        _registry.GetProvider("unknown").Returns((IExternalLinkProvider?)null);

        var result = await _sut.GetContentAsync("unknown", "some-id", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContent_ProviderThrows_ReturnsNullAndSwallowsException()
    {
        var provider = Substitute.For<IExternalLinkProvider>();
        provider.GetContentAsync("/api/spells/fireball", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        _registry.GetProvider("srd").Returns(provider);

        var result = await _sut.GetContentAsync("srd", "/api/spells/fireball", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContent_ValidProvider_ReturnsContentAndCaches()
    {
        var expected = new ExternalLinkContent
        {
            Source = "srd",
            Id = "/api/spells/fireball",
            Title = "Fireball",
            Kind = "spell",
            Markdown = "# Fireball\nDeals 8d6 fire damage."
        };

        var provider = Substitute.For<IExternalLinkProvider>();
        provider.GetContentAsync("/api/spells/fireball", Arg.Any<CancellationToken>())
            .Returns(expected);
        _registry.GetProvider("srd").Returns(provider);

        var result1 = await _sut.GetContentAsync("srd", "/api/spells/fireball", CancellationToken.None);
        Assert.NotNull(result1);
        Assert.Equal("Fireball", result1.Title);

        // Second call from cache
        var result2 = await _sut.GetContentAsync("srd", "/api/spells/fireball", CancellationToken.None);
        Assert.NotNull(result2);

        await provider.Received(1).GetContentAsync("/api/spells/fireball", Arg.Any<CancellationToken>());
    }

    // ── TryValidateSource ─────────────────────────────────────────

    [Fact]
    public void TryValidateSource_EmptySource_ReturnsFalse()
    {
        var valid = _sut.TryValidateSource("", out var error);

        Assert.False(valid);
        Assert.Equal("Source is required.", error);
    }

    [Fact]
    public void TryValidateSource_UnknownSource_NoProviders_ReturnsGenericError()
    {
        _registry.GetProvider("nope").Returns((IExternalLinkProvider?)null);
        _registry.GetAllProviders().Returns([]);

        var valid = _sut.TryValidateSource("nope", out var error);

        Assert.False(valid);
        Assert.Equal("Unknown external link source 'nope'.", error);
    }

    [Fact]
    public void TryValidateSource_UnknownSource_WithProviders_ListsAvailable()
    {
        var srdProvider = Substitute.For<IExternalLinkProvider>();
        srdProvider.Key.Returns("srd");

        _registry.GetProvider("nope").Returns((IExternalLinkProvider?)null);
        _registry.GetAllProviders().Returns([srdProvider]);

        var valid = _sut.TryValidateSource("nope", out var error);

        Assert.False(valid);
        Assert.Contains("srd", error);
    }

    [Fact]
    public void TryValidateSource_KnownSource_ReturnsTrue()
    {
        var provider = Substitute.For<IExternalLinkProvider>();
        _registry.GetProvider("srd").Returns(provider);

        var valid = _sut.TryValidateSource("srd", out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    // ── TryValidateId ─────────────────────────────────────────────

    [Fact]
    public void TryValidateId_EmptyId_ReturnsFalse()
    {
        var valid = _sut.TryValidateId("srd", "", out var error);

        Assert.False(valid);
        Assert.Equal("Id is required.", error);
    }

    [Fact]
    public void TryValidateId_AbsoluteUri_ReturnsFalse()
    {
        var valid = _sut.TryValidateId("srd", "https://example.com/spells", out var error);

        Assert.False(valid);
        Assert.Contains("relative path", error);
    }

    [Fact]
    public void TryValidateId_SrdWithoutApiPrefix_ReturnsFalse()
    {
        var valid = _sut.TryValidateId("srd", "spells/fireball", out var error);

        Assert.False(valid);
        Assert.Equal("SRD ids must start with /api/.", error);
    }

    [Fact]
    public void TryValidateId_NonSrdRelativePath_ReturnsTrue()
    {
        var valid = _sut.TryValidateId("blob", "monsters/goblin", out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }
}
