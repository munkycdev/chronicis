using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class WikiLinkCommitServiceTests
{
    private readonly IWikiLinkService _wikiLinkService;
    private readonly WikiLinkCommitService _sut;

    public WikiLinkCommitServiceTests()
    {
        _wikiLinkService = Substitute.For<IWikiLinkService>();
        _sut = new WikiLinkCommitService(_wikiLinkService);
    }

    // ── Decide ───────────────────────────────────────────────────────

    [Fact]
    public void Decide_DelegatesToStaticHelper_ReturnsSelectExisting()
    {
        var result = _sut.Decide("war", 3, 2, false, false);

        var select = Assert.IsType<AutocompleteCommitDecision.SelectExisting>(result);
        Assert.Equal(2, select.Index);
    }

    [Fact]
    public void Decide_DelegatesToStaticHelper_ReturnsCreateNew()
    {
        var result = _sut.Decide("waterdeep", 0, 0, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("Waterdeep", create.Name);
    }

    [Fact]
    public void Decide_DelegatesToStaticHelper_ReturnsDoNothing()
    {
        var result = _sut.Decide("wa", 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    // ── CreateAndLinkAsync ───────────────────────────────────────────

    [Fact]
    public async Task CreateAndLinkAsync_WhenServiceReturnsArticle_ReturnsSuccess()
    {
        var worldId = Guid.NewGuid();
        var article = new ArticleDto { Id = Guid.NewGuid(), Title = "Waterdeep" };
        _wikiLinkService.CreateArticleFromAutocompleteAsync("Waterdeep", worldId)
            .Returns(article);

        var result = await _sut.CreateAndLinkAsync("Waterdeep", worldId);

        Assert.True(result.Success);
        Assert.Equal(article, result.Article);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAndLinkAsync_WhenServiceReturnsNull_ReturnsFailure()
    {
        var worldId = Guid.NewGuid();
        _wikiLinkService.CreateArticleFromAutocompleteAsync("Waterdeep", worldId)
            .Returns((ArticleDto?)null);

        var result = await _sut.CreateAndLinkAsync("Waterdeep", worldId);

        Assert.False(result.Success);
        Assert.Null(result.Article);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAndLinkAsync_WhenServiceThrows_ReturnsFailure()
    {
        var worldId = Guid.NewGuid();
        _wikiLinkService.CreateArticleFromAutocompleteAsync("Waterdeep", worldId)
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _sut.CreateAndLinkAsync("Waterdeep", worldId);

        Assert.False(result.Success);
        Assert.Null(result.Article);
        Assert.Equal("boom", result.ErrorMessage);
    }
}
