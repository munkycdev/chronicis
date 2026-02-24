using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Articles;
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

public class ArticlesTests : MudBlazorTestContext
{
    [Fact]
    public void Articles_Render_WithSelectedArticle_RendersArticleDetail()
    {
        var tree = Substitute.For<ITreeStateService>();
        tree.SelectedArticleId.Returns(Guid.NewGuid());
        var api = Substitute.For<IArticleApiService>();

        var cut = CreateRenderedSut(tree, api);

        Assert.DoesNotContain("Loading article...", cut.Markup);
    }

    [Fact]
    public void Articles_Render_WithNoSelection_RendersLoadingState()
    {
        var tree = Substitute.For<ITreeStateService>();
        tree.SelectedArticleId.Returns((Guid?)null);
        var api = Substitute.For<IArticleApiService>();

        var cut = CreateRenderedSut(tree, api);

        Assert.Contains("Loading article...", cut.Markup);
    }

    [Fact]
    public async Task Articles_OnTreeStateChanged_InvokesStateHasChangedWithoutThrowing()
    {
        var tree = Substitute.For<ITreeStateService>();
        tree.SelectedArticleId.Returns((Guid?)null);
        var api = Substitute.For<IArticleApiService>();
        var cut = CreateRenderedSut(tree, api);

        var ex = await Record.ExceptionAsync(() =>
            cut.InvokeAsync(() => InvokePrivateSync(cut.Instance, "OnTreeStateChanged")));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Articles_OnInitialized_WithPathFound_ExpandsSelection()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var articleId = Guid.NewGuid();

        tree.SelectedArticleId.Returns((Guid?)null);
        api.GetArticleByPathAsync("world/entry").Returns(new ArticleDto { Id = articleId });

        var sut = CreateSut(tree, api, nav, "world/entry");

        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        tree.Received(1).ExpandPathToAndSelect(articleId);
    }

    [Fact]
    public async Task Articles_OnInitialized_PathMissing_RedirectsToDashboard()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        api.GetArticleByPathAsync("missing").Returns((ArticleDto?)null);

        var sut = CreateSut(tree, api, nav, "missing");
        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        Assert.EndsWith("/dashboard", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Articles_OnParametersSet_PathError_RedirectsToDashboard()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        api.GetArticleByPathAsync("broken").Returns(Task.FromException<ArticleDto?>(new Exception("boom")));

        var sut = CreateSut(tree, api, nav, "broken");
        await InvokeProtectedAsync(sut, "OnParametersSetAsync");

        Assert.EndsWith("/dashboard", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Articles_OnInitialized_NoPath_DoesNotCallGetByPath()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var sut = CreateSut(tree, api, nav, null);

        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        await api.DidNotReceive().GetArticleByPathAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Articles_OnInitialized_PathFound_AlreadySelected_DoesNotExpand()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var articleId = Guid.NewGuid();

        tree.SelectedArticleId.Returns((Guid?)articleId);
        api.GetArticleByPathAsync("world/entry").Returns(new ArticleDto { Id = articleId });

        var sut = CreateSut(tree, api, nav, "world/entry");
        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        tree.DidNotReceive().ExpandPathToAndSelect(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Articles_OnParametersSet_WithPathFound_ExpandsSelection()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var articleId = Guid.NewGuid();

        tree.SelectedArticleId.Returns((Guid?)null);
        api.GetArticleByPathAsync("world/entry").Returns(new ArticleDto { Id = articleId });

        var sut = CreateSut(tree, api, nav, "world/entry");
        await InvokeProtectedAsync(sut, "OnParametersSetAsync");

        tree.Received(1).ExpandPathToAndSelect(articleId);
    }

    [Fact]
    public async Task Articles_OnParametersSet_NoPath_DoesNotCallGetByPath()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var sut = CreateSut(tree, api, nav, null);

        await InvokeProtectedAsync(sut, "OnParametersSetAsync");

        await api.DidNotReceive().GetArticleByPathAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Articles_NavigateToArticle_UsesBreadcrumbPath()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var id = Guid.NewGuid();
        api.GetArticleDetailAsync(id).Returns(new ArticleDto
        {
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }, new BreadcrumbDto { Slug = "article" }]
        });

        var sut = CreateSut(tree, api, nav, null);
        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.EndsWith("/article/world/article", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Articles_NavigateToArticle_WhenArticleMissing_DoesNotNavigate()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var id = Guid.NewGuid();
        api.GetArticleDetailAsync(id).Returns((ArticleDto?)null);
        var before = nav.Uri;

        var sut = CreateSut(tree, api, nav, null);
        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Articles_NavigateToArticle_WhenBreadcrumbsEmpty_DoesNotNavigate()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var id = Guid.NewGuid();
        api.GetArticleDetailAsync(id).Returns(new ArticleDto { Id = id, Breadcrumbs = new List<BreadcrumbDto>() });
        var before = nav.Uri;

        var sut = CreateSut(tree, api, nav, null);
        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public void Articles_Dispose_UnsubscribesFromStateChanged()
    {
        var tree = Substitute.For<ITreeStateService>();
        var api = Substitute.For<IArticleApiService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var sut = CreateSut(tree, api, nav, null);

        sut.Dispose();

        tree.Received().OnStateChanged -= Arg.Any<Action>();
    }

    private static Articles CreateSut(ITreeStateService tree, IArticleApiService api, NavigationManager nav, string? path)
    {
        var sut = new Articles(Substitute.For<ILogger<Articles>>());
        SetProperty(sut, "TreeStateService", tree);
        SetProperty(sut, "ArticleApi", api);
        SetProperty(sut, "QuoteService", Substitute.For<IQuoteService>());
        SetProperty(sut, "Navigation", nav);
        SetProperty(sut, nameof(Articles.Path), path);

        return sut;
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

    private static void InvokePrivateSync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
    }

    private IRenderedComponent<Articles> CreateRenderedSut(ITreeStateService tree, IArticleApiService api, string? path = null)
    {
        ComponentFactories.AddStub<ArticleDetail>();
        Services.AddSingleton(tree);
        Services.AddSingleton(api);
        Services.AddSingleton(Substitute.For<IQuoteService>());
        Services.AddSingleton(Substitute.For<ILogger<Articles>>());

        return RenderComponent<Articles>(parameters =>
        {
            if (path != null)
            {
                parameters.Add(p => p.Path, path);
            }
        });
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }
}
