using System.Reflection;
using Bunit.TestDoubles;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class SearchTests : MudBlazorTestContext
{
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

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }
}
