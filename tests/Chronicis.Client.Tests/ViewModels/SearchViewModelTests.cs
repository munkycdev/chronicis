using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class SearchViewModelTests
{
    private static (SearchViewModel Sut,
        ISearchApiService SearchApi,
        IBreadcrumbService BreadcrumbService,
        ITreeStateService TreeState,
        IAppNavigator Navigator) CreateSut()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var treeState = Substitute.For<ITreeStateService>();
        var navigator = Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<SearchViewModel>>();

        var sut = new SearchViewModel(searchApi, breadcrumbs, treeState, navigator, logger);
        return (sut, searchApi, breadcrumbs, treeState, navigator);
    }

    private static GlobalSearchResultsDto MakeResults(string query = "magic", int count = 2)
    {
        var articles = Enumerable.Range(0, count)
            .Select(i => new ArticleSearchResultDto
            {
                Id = Guid.NewGuid(),
                Title = $"Result {i}",
                Slug = $"result-{i}",
                AncestorPath = null,
            })
            .ToList();

        return new GlobalSearchResultsDto
        {
            Query = query,
            TitleMatches = articles,
            BodyMatches = new(),
            HashtagMatches = new(),
            TotalResults = count,
        };
    }

    // ---------------------------------------------------------------------------
    // Initial state
    // ---------------------------------------------------------------------------

    [Fact]
    public void InitialState_IsLoadingFalse_ResultsNull()
    {
        var (sut, _, _, _, _) = CreateSut();
        Assert.False(sut.IsLoading);
        Assert.Null(sut.Results);
    }

    // ---------------------------------------------------------------------------
    // SearchAsync
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_NullOrWhitespaceQuery_ClearsResultsAndDoesNotCallApi(string? query)
    {
        var (sut, searchApi, _, _, _) = CreateSut();

        await sut.SearchAsync(query);

        Assert.Null(sut.Results);
        await searchApi.DidNotReceive().SearchContentAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_CallsApiAndSetsResults()
    {
        var (sut, searchApi, _, _, _) = CreateSut();
        var expected = MakeResults("dragon");
        searchApi.SearchContentAsync("dragon").Returns(expected);

        await sut.SearchAsync("dragon");

        Assert.Equal(expected, sut.Results);
    }

    [Fact]
    public async Task SearchAsync_SetsIsLoadingTrue_DuringRequest_ThenFalse()
    {
        var (sut, searchApi, _, _, _) = CreateSut();
        var loadingDuringCall = false;

        searchApi.SearchContentAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                loadingDuringCall = sut.IsLoading;
                return Task.FromResult<GlobalSearchResultsDto?>(MakeResults());
            });

        await sut.SearchAsync("test");

        Assert.True(loadingDuringCall);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task SearchAsync_RaisesPropertyChangedForIsLoading()
    {
        var (sut, searchApi, _, _, _) = CreateSut();
        searchApi.SearchContentAsync(Arg.Any<string>()).Returns(MakeResults());
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await sut.SearchAsync("elf");

        Assert.Contains(nameof(sut.IsLoading), changed);
        Assert.Contains(nameof(sut.Results), changed);
    }

    [Fact]
    public async Task SearchAsync_WhenApiThrows_SetsResultsNullAndClearsLoading()
    {
        var (sut, searchApi, _, _, _) = CreateSut();
        searchApi.SearchContentAsync(Arg.Any<string>()).ThrowsAsync(new HttpRequestException("network"));

        await sut.SearchAsync("goblin");

        Assert.Null(sut.Results);
        Assert.False(sut.IsLoading);
    }

    // ---------------------------------------------------------------------------
    // NavigateToArticle
    // ---------------------------------------------------------------------------

    [Fact]
    public void NavigateToArticle_WithAncestorPath_BuildsUrlAndNavigates()
    {
        var (sut, _, breadcrumbs, treeState, navigator) = CreateSut();
        var ancestors = new List<BreadcrumbDto> { new() { Slug = "world" }, new() { Slug = "magic" } };
        var result = new ArticleSearchResultDto { Id = Guid.NewGuid(), Slug = "fire", AncestorPath = ancestors };
        breadcrumbs.BuildArticleUrl(ancestors).Returns("/article/world/magic");

        sut.NavigateToArticle(result);

        treeState.Received(1).ExpandPathToAndSelect(result.Id);
        navigator.Received(1).NavigateTo("/article/world/magic");
    }

    [Fact]
    public void NavigateToArticle_WithNoAncestorPath_UsesSlugFallback()
    {
        var (sut, _, _, treeState, navigator) = CreateSut();
        var result = new ArticleSearchResultDto { Id = Guid.NewGuid(), Slug = "fireball", AncestorPath = null };

        sut.NavigateToArticle(result);

        treeState.Received(1).ExpandPathToAndSelect(result.Id);
        navigator.Received(1).NavigateTo("/article/fireball");
    }

    [Fact]
    public void NavigateToArticle_WithEmptyAncestorPath_UsesSlugFallback()
    {
        var (sut, _, _, treeState, navigator) = CreateSut();
        var result = new ArticleSearchResultDto
        {
            Id = Guid.NewGuid(),
            Slug = "fireball",
            AncestorPath = new List<BreadcrumbDto>()
        };

        sut.NavigateToArticle(result);

        navigator.Received(1).NavigateTo("/article/fireball");
    }
}
