using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class SearchTests : MudBlazorTestContext
{
    [Fact]
    public void Search_Rendered_NoQuery_ShowsPrompt()
    {
        var _ = CreateRenderedSut();

        var cut = RenderComponent<Search>();

        Assert.Contains("Enter a search term to begin", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Search_Rendered_WithZeroResults_ShowsNoResultsMessage()
    {
        var (searchApi, _, _, navigation, _) = CreateRenderedSut();
        searchApi.SearchContentAsync("acid").Returns(new GlobalSearchResultsDto
        {
            Query = "acid",
            TotalResults = 0
        });

        var uri = navigation.GetUriWithQueryParameter("q", "acid");
        navigation.NavigateTo(uri);

        var cut = RenderComponent<Search>();

        Assert.Contains("No results found for", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("acid", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Search_Rendered_WithResults_ShowsAllGroups()
    {
        var (searchApi, _, _, navigation, _) = CreateRenderedSut();
        searchApi.SearchContentAsync("term").Returns(new GlobalSearchResultsDto
        {
            Query = "term",
            TotalResults = 3,
            TitleMatches = [new ArticleSearchResultDto { Id = Guid.NewGuid(), Title = "T", Slug = "t", MatchSnippet = "m", MatchType = "title" }],
            BodyMatches = [new ArticleSearchResultDto { Id = Guid.NewGuid(), Title = "B", Slug = "b", MatchSnippet = "m", MatchType = "content" }],
            HashtagMatches = [new ArticleSearchResultDto { Id = Guid.NewGuid(), Title = "H", Slug = "h", MatchSnippet = "m", MatchType = "hashtag" }]
        });

        var uri = navigation.GetUriWithQueryParameter("q", "term");
        navigation.NavigateTo(uri);

        var cut = RenderComponent<Search>();

        Assert.Contains("Search Results", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Title Matches (1)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content Matches (1)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hashtag Matches (1)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("results for", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_PerformSearch_WhenApiThrows_ResetsResultsAndLoading()
    {
        var (searchApi, _, _, _, _) = CreateRenderedSut();
        var cut = RenderComponent<Search>();
        cut.Instance.SearchQuery = "boom";
        searchApi.SearchContentAsync("boom").Returns(Task.FromException<GlobalSearchResultsDto>(new Exception("fail")));

        await InvokePrivateOnRendererAsync(cut, "PerformSearch");

        await searchApi.Received(1).SearchContentAsync("boom");
        Assert.Null(GetPrivateField<GlobalSearchResultsDto>(cut.Instance, "_results"));
        Assert.False(GetPrivateFieldValue<bool>(cut.Instance, "_isLoading"));
    }

    [Fact]
    public async Task Search_PerformSearch_WithQuery_CallsSearch()
    {
        var (searchApi, _, _, _, _) = CreateRenderedSut();
        var cut = RenderComponent<Search>();
        cut.Instance.SearchQuery = "dragon";
        searchApi.SearchContentAsync("dragon").Returns(new GlobalSearchResultsDto
        {
            Query = "dragon",
            TotalResults = 1
        });

        await InvokePrivateOnRendererAsync(cut, "PerformSearch");

        await searchApi.Received(1).SearchContentAsync("dragon");
    }

    [Fact]
    public async Task Search_OnParametersSet_NoQuery_ClearsResults()
    {
        var (sut, searchApi, _, _, _) = CreateSut();
        sut.SearchQuery = " ";

        await InvokeProtectedAsync(sut, "OnParametersSetAsync");

        await searchApi.DidNotReceive().SearchContentAsync(Arg.Any<string>());
        Assert.Null(GetPrivateField<GlobalSearchResultsDto>(sut, "_results"));
    }

    [Fact]
    public async Task Search_NavigateToArticle_UsesAncestorPathWhenAvailable()
    {
        var (sut, _, breadcrumbService, nav, tree) = CreateSut();
        breadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/path");

        var result = new ArticleSearchResultDto
        {
            Id = Guid.NewGuid(),
            Slug = "fallback",
            AncestorPath = [new BreadcrumbDto { Slug = "world" }]
        };

        await InvokePrivateAsync(sut, "NavigateToArticle", result);

        tree.Received(1).ExpandPathToAndSelect(result.Id);
        Assert.EndsWith("/article/world/path", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_NavigateToArticle_UsesSlugWhenNoAncestors()
    {
        var (sut, _, _, nav, tree) = CreateSut();
        var result = new ArticleSearchResultDto
        {
            Id = Guid.NewGuid(),
            Slug = "by-slug",
            AncestorPath = []
        };

        await InvokePrivateAsync(sut, "NavigateToArticle", result);

        tree.Received(1).ExpandPathToAndSelect(result.Id);
        Assert.EndsWith("/article/by-slug", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_NavigateToArticle_WithNullAncestorPath_UsesSlug()
    {
        var (sut, _, _, nav, tree) = CreateSut();
        var result = new ArticleSearchResultDto
        {
            Id = Guid.NewGuid(),
            Slug = "null-path",
            AncestorPath = null!
        };

        await InvokePrivateAsync(sut, "NavigateToArticle", result);

        tree.Received(1).ExpandPathToAndSelect(result.Id);
        Assert.EndsWith("/article/null-path", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    private (Search Sut, ISearchApiService SearchApi, IBreadcrumbService BreadcrumbService, FakeNavigationManager Nav, ITreeStateService Tree) CreateSut()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var tree = Substitute.For<ITreeStateService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var sut = new Search(Substitute.For<ILogger<Search>>());
        SetProperty(sut, "SearchApi", searchApi);
        SetProperty(sut, "BreadcrumbService", breadcrumbService);
        SetProperty(sut, "TreeState", tree);
        SetProperty(sut, "Navigation", nav);

        return (sut, searchApi, breadcrumbService, nav, tree);
    }

    private (ISearchApiService SearchApi, IBreadcrumbService BreadcrumbService, ITreeStateService TreeState, NavigationManager Navigation, ILogger<Search> Logger) CreateRenderedSut()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var treeState = Substitute.For<ITreeStateService>();
        var logger = Substitute.For<ILogger<Search>>();

        Services.AddSingleton(searchApi);
        Services.AddSingleton(breadcrumbService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(logger);

        var navigation = Services.GetRequiredService<NavigationManager>();
        return (searchApi, breadcrumbService, treeState, navigation, logger);
    }

    private static async Task InvokeProtectedAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task?)method!.Invoke(instance, null);
        if (task != null)
        {
            await task;
        }
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task?)method!.Invoke(instance, args);
        if (task != null)
        {
            await task;
        }
    }

    private static T? GetPrivateField<T>(object instance, string fieldName) where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return field!.GetValue(instance) as T;
    }

    private static T GetPrivateFieldValue<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<Search> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }
}
