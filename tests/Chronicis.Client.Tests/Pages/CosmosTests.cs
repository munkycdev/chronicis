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
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class CosmosTests : MudBlazorTestContext
{
    [Fact]
    public void Cosmos_Rendered_WhenSelectedArticle_RendersArticleDetailBranch()
    {
        var (articleApi, treeState, quoteService, _, _) = CreateRenderedServices();
        treeState.SelectedArticleId.Returns(Guid.NewGuid());

        ComponentFactories.Add(
            t => t == typeof(ArticleDetail),
            _ => new ArticleDetailStub());

        var cut = RenderComponent<Cosmos>();

        Assert.Contains("article-detail-stub", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Your Chronicle Awaits", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenFoundAndAlreadySelected_DoesNotExpand()
    {
        var (sut, articleApi, treeState, _, _, _) = CreateSut();
        var id = Guid.NewGuid();
        treeState.SelectedArticleId.Returns(id);
        articleApi.GetArticleByPathAsync("path/here").Returns(new ArticleDto { Id = id });

        await InvokePrivateAsync(sut, "LoadArticleByPath", "path/here");

        treeState.DidNotReceive().ExpandPathToAndSelect(id);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenFoundAndDifferentSelected_ExpandsSelection()
    {
        var (sut, articleApi, treeState, _, _, _) = CreateSut();
        var selectedId = Guid.NewGuid();
        var foundId = Guid.NewGuid();
        treeState.SelectedArticleId.Returns(selectedId);
        articleApi.GetArticleByPathAsync("path/here").Returns(new ArticleDto { Id = foundId });

        await InvokePrivateAsync(sut, "LoadArticleByPath", "path/here");

        treeState.Received(1).ExpandPathToAndSelect(foundId);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenNotFoundButSelected_DoesNotNavigate()
    {
        var (sut, articleApi, treeState, _, nav, _) = CreateSut();
        treeState.SelectedArticleId.Returns(Guid.NewGuid());
        articleApi.GetArticleByPathAsync("missing").Returns((ArticleDto?)null);
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "LoadArticleByPath", "missing");

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenNotFoundAndNoSelection_NavigatesHome()
    {
        var (sut, articleApi, treeState, _, nav, _) = CreateSut();
        treeState.SelectedArticleId.Returns((Guid?)null);
        articleApi.GetArticleByPathAsync("missing").Returns((ArticleDto?)null);

        await InvokePrivateAsync(sut, "LoadArticleByPath", "missing");

        Assert.EndsWith("/", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenExceptionAndSelected_DoesNotNavigate()
    {
        var (sut, articleApi, treeState, _, nav, _) = CreateSut();
        treeState.SelectedArticleId.Returns(Guid.NewGuid());
        articleApi.GetArticleByPathAsync("boom").Returns(Task.FromException<ArticleDto?>(new Exception("boom")));
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "LoadArticleByPath", "boom");

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Cosmos_LoadArticleByPath_WhenExceptionAndNoSelection_NavigatesHome()
    {
        var (sut, articleApi, treeState, _, nav, _) = CreateSut();
        treeState.SelectedArticleId.Returns((Guid?)null);
        articleApi.GetArticleByPathAsync("boom").Returns(Task.FromException<ArticleDto?>(new Exception("boom")));

        await InvokePrivateAsync(sut, "LoadArticleByPath", "boom");

        Assert.EndsWith("/", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_LoadDashboardData_WhenNoRoots_SetsZeroDaysSinceStart()
    {
        var (articleApi, _, _, _, _) = CreateRenderedServices();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());

        var cut = RenderComponent<Cosmos>();
        await InvokePrivateOnRendererAsync(cut, "LoadDashboardData");

        var stats = GetPrivateField<object>(cut.Instance, "_stats");
        var days = GetProperty<int>(stats!, "DaysSinceStart");
        var roots = GetProperty<int>(stats!, "RootArticles");

        Assert.Equal(0, days);
        Assert.Equal(0, roots);
    }

    [Fact]
    public async Task Cosmos_LoadDashboardData_WhenApiThrows_StopsLoading()
    {
        var (articleApi, _, _, _, _) = CreateRenderedServices();
        articleApi.GetRootArticlesAsync().Returns(Task.FromException<List<ArticleTreeDto>>(new Exception("boom")));

        var cut = RenderComponent<Cosmos>();

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(cut, "LoadDashboardData"));

        Assert.Null(ex);
        Assert.False(GetPrivateField<bool>(cut.Instance, "_isLoadingRecent"));
    }

    [Fact]
    public async Task Cosmos_LoadQuote_WhenServiceThrows_StopsLoading()
    {
        var (_, _, quoteService, _, _) = CreateRenderedServices();
        quoteService.GetRandomQuoteAsync().Returns(Task.FromException<Quote?>(new Exception("boom")));

        var cut = RenderComponent<Cosmos>();

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(cut, "LoadQuote"));

        Assert.Null(ex);
        Assert.False(GetPrivateField<bool>(cut.Instance, "_loadingQuote"));
    }

    [Fact]
    public async Task Cosmos_LoadNewQuote_DelegatesToLoadQuote()
    {
        var (_, _, quoteService, _, _) = CreateRenderedServices();
        quoteService.GetRandomQuoteAsync().Returns(new Quote { Content = "fresh", Author = "author" });

        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<Cosmos>();

        await InvokePrivateOnRendererAsync(cut, "LoadNewQuote");

        Assert.Equal("fresh", GetPrivateField<Quote>(cut.Instance, "_quote").Content);
    }

    [Fact]
    public async Task Cosmos_OnInitializedAsync_WithPath_LoadsArticleByPath()
    {
        var (articleApi, _, _, _, _) = CreateRenderedServices();
        articleApi.GetArticleByPathAsync("world/path").Returns(new ArticleDto { Id = Guid.NewGuid() });
        RenderComponent<Cosmos>(parameters => parameters.Add(p => p.Path, "world/path"));

        await articleApi.Received().GetArticleByPathAsync("world/path");
    }

    [Fact]
    public async Task Cosmos_OnParametersSetAsync_WithPath_LoadsArticleByPath()
    {
        var (sut, articleApi, _, _, _, _) = CreateSut();
        sut.Path = "world/path";
        articleApi.GetArticleByPathAsync("world/path").Returns(new ArticleDto { Id = Guid.NewGuid() });

        await InvokeProtectedAsync(sut, "OnParametersSetAsync");

        await articleApi.Received(1).GetArticleByPathAsync("world/path");
    }

    [Fact]
    public void Cosmos_Rendered_WithStatsAndQuote_ShowsStatsAndQuoteFooter()
    {
        var (_, treeState, _, _, _) = CreateRenderedServices();
        treeState.SelectedArticleId.Returns((Guid?)null);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<Cosmos>();
        SetPrivateField(cut.Instance, "_isLoadingRecent", false);
        SetPrivateField(cut.Instance, "_recentArticles", new List<ArticleDto>());

        var statsType = cut.Instance.GetType().GetNestedType("CampaignStats", BindingFlags.NonPublic);
        Assert.NotNull(statsType);
        var stats = Activator.CreateInstance(statsType!);
        Assert.NotNull(stats);
        SetProperty(stats!, "TotalArticles", 12);
        SetProperty(stats!, "RootArticles", 3);
        SetProperty(stats!, "RecentlyModified", 4);
        SetProperty(stats!, "DaysSinceStart", 9);
        SetPrivateField(cut.Instance, "_stats", stats);

        SetPrivateField(cut.Instance, "_loadingQuote", false);
        SetPrivateField(cut.Instance, "_quote", new Quote { Content = "Quote body", Author = "Author name" });
        cut.Render();

        Assert.Contains("Total Articles", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Top-Level Topics", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Edited This Week", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Days Chronicling", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Quote body", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Author name", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_OnTreeStateChanged_DoesNotThrow()
    {
        CreateRenderedServices();
        var cut = RenderComponent<Cosmos>();

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(cut, "OnTreeStateChanged"));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(10, "10m ago")]
    [InlineData(120, "2h ago")]
    [InlineData(60 * 24 * 3, "3d ago")]
    [InlineData(60 * 24 * 10, "1w ago")]
    public async Task Cosmos_FormatRelativeTime_FormatsExpectedRecentRanges(int minutesAgo, string expected)
    {
        var sut = CreateSut().Sut;
        var date = DateTime.Now.AddMinutes(-minutesAgo);

        var actual = await InvokePrivateWithResultAsync<string>(sut, "FormatRelativeTime", date);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Cosmos_FormatRelativeTime_OlderThanThirtyDays_UsesDateFormat()
    {
        var sut = CreateSut().Sut;
        var date = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Local);

        var actual = await InvokePrivateWithResultAsync<string>(sut, "FormatRelativeTime", date);

        Assert.Equal("Jan 15, 2023", actual);
    }

    [Fact]
    public async Task Cosmos_CountTotalArticles_CountsRecursively()
    {
        var sut = CreateSut().Sut;

        var tree = new List<ArticleTreeDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Children =
                [
                    new ArticleTreeDto
                    {
                        Id = Guid.NewGuid(),
                        Children = [new ArticleTreeDto { Id = Guid.NewGuid() }]
                    }
                ]
            },
            new() { Id = Guid.NewGuid() }
        };

        var count = await InvokePrivateWithResultAsync<int>(sut, "CountTotalArticles", tree);

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Cosmos_GetRecentArticlesRecursive_FiltersNull_Recurses_OrdersAndLimits()
    {
        var (sut, articleApi, _, _, _, _) = CreateSut();
        var root1 = Guid.NewGuid();
        var root2 = Guid.NewGuid();
        var child = Guid.NewGuid();
        var now = DateTime.Now;

        articleApi.GetArticleAsync(root1).Returns(new ArticleDto { Id = root1, CreatedAt = now.AddDays(-3), ModifiedAt = now.AddDays(-2) });
        articleApi.GetArticleAsync(root2).Returns((ArticleDto?)null);
        articleApi.GetArticleAsync(child).Returns(new ArticleDto { Id = child, CreatedAt = now.AddDays(-1), ModifiedAt = now.AddHours(-3) });

        var tree = new List<ArticleTreeDto>
        {
            new()
            {
                Id = root1,
                HasChildren = true,
                Children = [new ArticleTreeDto { Id = child }]
            },
            new() { Id = root2 }
        };

        var results = await InvokePrivateWithResultAsync<List<ArticleDto>>(sut, "GetRecentArticlesRecursive", tree, 1);

        Assert.Single(results);
        Assert.Equal(child, results[0].Id);
    }

    [Fact]
    public async Task Cosmos_LoadDashboardData_WithArticles_ComputesRecentlyModifiedAndDays()
    {
        var (articleApi, _, _, _, _) = CreateRenderedServices();
        var rootA = Guid.NewGuid();
        var rootB = Guid.NewGuid();
        var now = DateTime.Now;

        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = rootA, CreatedAt = now.AddDays(-10) },
            new() { Id = rootB, CreatedAt = now.AddDays(-1) }
        });
        articleApi.GetArticleAsync(rootA).Returns(new ArticleDto { Id = rootA, CreatedAt = now.AddDays(-10), ModifiedAt = now.AddDays(-8) });
        articleApi.GetArticleAsync(rootB).Returns(new ArticleDto { Id = rootB, CreatedAt = now.AddDays(-1), ModifiedAt = now.AddDays(-1) });

        var cut = RenderComponent<Cosmos>();
        await InvokePrivateOnRendererAsync(cut, "LoadDashboardData");

        var stats = GetPrivateField<object>(cut.Instance, "_stats");
        Assert.Equal(2, GetProperty<int>(stats!, "TotalArticles"));
        Assert.Equal(2, GetProperty<int>(stats!, "RootArticles"));
        Assert.Equal(1, GetProperty<int>(stats!, "RecentlyModified"));
        Assert.True(GetProperty<int>(stats!, "DaysSinceStart") >= 9);
    }

    [Fact]
    public async Task Cosmos_NavigateToArticle_WithBreadcrumbs_UsesBreadcrumbServiceUrl()
    {
        var (sut, articleApi, _, _, nav, breadcrumbService) = CreateSut();
        var id = Guid.NewGuid();

        articleApi.GetArticleDetailAsync(id).Returns(new ArticleDto
        {
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }]
        });
        breadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/path");

        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.EndsWith("/article/world/path", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_NavigateToArticle_WithoutBreadcrumbs_DoesNotNavigate()
    {
        var (sut, articleApi, _, _, nav, _) = CreateSut();
        var id = Guid.NewGuid();

        articleApi.GetArticleDetailAsync(id).Returns(new ArticleDto());
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Cosmos_CreateFirstArticle_CreatesEmptyTitleAndSelectsCreatedArticle()
    {
        var (sut, articleApi, treeState, _, _, _) = CreateSut();
        var createdId = Guid.NewGuid();

        articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(new ArticleDto { Id = createdId });

        await InvokePrivateAsync(sut, "CreateFirstArticle");

        await articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto => dto.Title == string.Empty));
        await treeState.Received(1).RefreshAsync();
        treeState.Received(1).ExpandPathToAndSelect(createdId);
    }

    [Fact]
    public async Task Cosmos_CreateArticleWithTitle_WhenCreateFails_DoesNotRefreshTree()
    {
        var (sut, articleApi, treeState, _, _, _) = CreateSut();

        articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns((ArticleDto?)null);

        await InvokePrivateAsync(sut, "CreateArticleWithTitle", "New Character");

        await treeState.DidNotReceive().RefreshAsync();
        treeState.DidNotReceive().ExpandPathToAndSelect(Arg.Any<Guid>());
    }

    [Fact]
    public void Cosmos_Render_WithRecentArticles_ShowsUntitledAndNavigatesOnClick()
    {
        var (articleApi, treeState, _, _, breadcrumbService) = CreateRenderedServices();
        var articleId = Guid.NewGuid();
        treeState.SelectedArticleId.Returns((Guid?)null);
        articleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }, new BreadcrumbDto { Slug = "entry" }]
        });
        breadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/entry");

        var cut = RenderComponent<Cosmos>();
        SetPrivateField(cut.Instance, "_isLoadingRecent", false);
        SetPrivateField(cut.Instance, "_recentArticles", new List<ArticleDto>
        {
            new() { Id = articleId, Title = "", IconEmoji = null, CreatedAt = DateTime.Now.AddDays(-1) }
        });
        SetPrivateField(cut.Instance, "_loadingQuote", false);
        SetPrivateField(cut.Instance, "_quote", (Quote?)null);
        cut.Render();

        Assert.Contains("(Untitled)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Modified", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Find(".mud-list-item").Click();

        Assert.EndsWith("/article/world/entry", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cosmos_Render_WithRecentArticleHavingTitle_ShowsProvidedTitle()
    {
        var (articleApi, treeState, _, _, _) = CreateRenderedServices();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var articleId = Guid.NewGuid();

        var cut = RenderComponent<Cosmos>();
        SetPrivateField(cut.Instance, "_isLoadingRecent", false);
        SetPrivateField(cut.Instance, "_recentArticles", new List<ArticleDto>
        {
            new() { Id = articleId, Title = "Named Article", CreatedAt = DateTime.Now.AddDays(-1) }
        });
        SetPrivateField(cut.Instance, "_loadingQuote", false);
        SetPrivateField(cut.Instance, "_quote", (Quote?)null);
        cut.Render();

        Assert.Contains("Named Article", cut.Markup, StringComparison.OrdinalIgnoreCase);
        articleApi.DidNotReceive().GetArticleDetailAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Cosmos_NavigateToArticle_WhenArticleDetailIsNull_DoesNotNavigate()
    {
        var (sut, articleApi, _, _, nav, _) = CreateSut();
        var id = Guid.NewGuid();
        articleApi.GetArticleDetailAsync(id).Returns((ArticleDto?)null);
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "NavigateToArticle", id);

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Cosmos_LoadDashboardData_RecentlyModified_UsesCreatedAtWhenModifiedMissing()
    {
        var (articleApi, _, _, _, _) = CreateRenderedServices();
        var rootA = Guid.NewGuid();
        var rootB = Guid.NewGuid();
        var now = DateTime.Now;

        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = rootA, CreatedAt = now.AddDays(-20) },
            new() { Id = rootB, CreatedAt = now.AddDays(-2) }
        });
        articleApi.GetArticleAsync(rootA).Returns(new ArticleDto { Id = rootA, CreatedAt = now.AddDays(-20), ModifiedAt = null });
        articleApi.GetArticleAsync(rootB).Returns(new ArticleDto { Id = rootB, CreatedAt = now.AddDays(-2), ModifiedAt = null });

        var cut = RenderComponent<Cosmos>();
        await InvokePrivateOnRendererAsync(cut, "LoadDashboardData");

        var stats = GetPrivateField<object>(cut.Instance, "_stats");
        Assert.Equal(1, GetProperty<int>(stats!, "RecentlyModified"));
    }

    [Fact]
    public async Task Cosmos_GetRecentArticlesRecursive_OrdersUsingCreatedAtWhenModifiedMissing()
    {
        var (sut, articleApi, _, _, _, _) = CreateSut();
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var now = DateTime.Now;

        articleApi.GetArticleAsync(olderId).Returns(new ArticleDto { Id = olderId, CreatedAt = now.AddDays(-10), ModifiedAt = null });
        articleApi.GetArticleAsync(newerId).Returns(new ArticleDto { Id = newerId, CreatedAt = now.AddDays(-1), ModifiedAt = null });

        var tree = new List<ArticleTreeDto>
        {
            new() { Id = olderId },
            new() { Id = newerId }
        };

        var results = await InvokePrivateWithResultAsync<List<ArticleDto>>(sut, "GetRecentArticlesRecursive", tree, 5);

        Assert.Equal(2, results.Count);
        Assert.Equal(newerId, results[0].Id);
    }

    [Fact]
    public void Cosmos_Render_QuickActionButtons_CreateExpectedTitles()
    {
        var (articleApi, treeState, _, _, _) = CreateRenderedServices();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var createdId = Guid.NewGuid();
        articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(new ArticleDto { Id = createdId });

        var cut = RenderComponent<Cosmos>();
        SetPrivateField(cut.Instance, "_isLoadingRecent", false);
        SetPrivateField(cut.Instance, "_recentArticles", new List<ArticleDto>());
        SetPrivateField(cut.Instance, "_loadingQuote", false);
        cut.Render();

        cut.FindAll("button").First(b => b.TextContent.Contains("Create Character", StringComparison.OrdinalIgnoreCase)).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Location", StringComparison.OrdinalIgnoreCase)).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Session Notes", StringComparison.OrdinalIgnoreCase)).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Lore Entry", StringComparison.OrdinalIgnoreCase)).Click();

        articleApi.Received().CreateArticleAsync(Arg.Is<ArticleCreateDto>(d => d.Title == "New Character"));
        articleApi.Received().CreateArticleAsync(Arg.Is<ArticleCreateDto>(d => d.Title == "New Location"));
        articleApi.Received().CreateArticleAsync(Arg.Is<ArticleCreateDto>(d => d.Title == "Session Notes"));
        articleApi.Received().CreateArticleAsync(Arg.Is<ArticleCreateDto>(d => d.Title == "Lore Entry"));
    }

    [Fact]
    public void Cosmos_Dispose_UnsubscribesFromTreeState()
    {
        var (sut, _, treeState, _, _, _) = CreateSut();

        sut.Dispose();

        treeState.Received().OnStateChanged -= Arg.Any<Action>();
    }

    private (Cosmos Sut, IArticleApiService ArticleApi, ITreeStateService TreeState, IQuoteService QuoteService, FakeNavigationManager Nav, IBreadcrumbService BreadcrumbService) CreateSut()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var quoteService = Substitute.For<IQuoteService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var sut = new Cosmos(Substitute.For<ILogger<Cosmos>>());
        SetProperty(sut, "ArticleApi", articleApi);
        SetProperty(sut, "TreeStateService", treeState);
        SetProperty(sut, "QuoteService", quoteService);
        SetProperty(sut, "BreadcrumbService", breadcrumbService);
        SetProperty(sut, "Navigation", nav);

        return (sut, articleApi, treeState, quoteService, nav, breadcrumbService);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task InvokeProtectedAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, null);
        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task<T> InvokePrivateWithResultAsync<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, args);

        if (result is Task<T> taskOfT)
        {
            return await taskOfT;
        }

        return (T)result!;
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private (IArticleApiService ArticleApi, ITreeStateService TreeState, IQuoteService QuoteService, NavigationManager Navigation, IBreadcrumbService BreadcrumbService) CreateRenderedServices()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var quoteService = Substitute.For<IQuoteService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var logger = Substitute.For<ILogger<Cosmos>>();

        treeState.SelectedArticleId.Returns((Guid?)null);
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        Services.AddSingleton(articleApi);
        Services.AddSingleton(treeState);
        Services.AddSingleton(quoteService);
        Services.AddSingleton(breadcrumbService);
        Services.AddSingleton(logger);

        var nav = Services.GetRequiredService<NavigationManager>();
        return (articleApi, treeState, quoteService, nav, breadcrumbService);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<Cosmos> cut, string methodName, params object[] args)
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

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        return (T)property!.GetValue(instance)!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private sealed class ArticleDetailStub : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "article-detail-stub");
            builder.CloseElement();
        }
    }
}
