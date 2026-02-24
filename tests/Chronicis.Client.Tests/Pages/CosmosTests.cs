using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Render-only tests for the Cosmos page shell.
/// Business logic is fully covered by CosmosViewModelTests.
/// These tests verify that the page renders the correct UI branches based on ViewModel state.
/// </summary>
public class CosmosTests : MudBlazorTestContext
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private CosmosViewModel CreateViewModel(
        IArticleApiService? articleApi = null,
        IQuoteService? quoteService = null,
        IBreadcrumbService? breadcrumbService = null,
        ITreeStateService? treeState = null,
        IAppNavigator? navigator = null)
    {
        articleApi ??= Substitute.For<IArticleApiService>();
        quoteService ??= Substitute.For<IQuoteService>();
        breadcrumbService ??= Substitute.For<IBreadcrumbService>();
        treeState ??= Substitute.For<ITreeStateService>();
        navigator ??= Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<CosmosViewModel>>();

        return new CosmosViewModel(
            articleApi, quoteService, breadcrumbService, treeState, navigator, logger);
    }

    private IRenderedComponent<Cosmos> RenderWithViewModel(
        CosmosViewModel vm,
        ITreeStateService? treeState = null)
    {
        treeState ??= Substitute.For<ITreeStateService>();
        Services.AddSingleton(vm);
        Services.AddSingleton(treeState);
        Services.AddSingleton(Substitute.For<ILogger<Cosmos>>());

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/");

        ComponentFactories.Add(
            t => t == typeof(ArticleDetail),
            _ => new ArticleDetailStub());

        // MudTooltip requires MudPopoverProvider; stub it to render child content only.
        ComponentFactories.AddStub<MudTooltip>(ps => ps.Get(p => p.ChildContent));

        return RenderComponent<Cosmos>();
    }

    /// <summary>
    /// Returns an IArticleApiService whose GetRootArticlesAsync never completes,
    /// so that InitializeAsync is suspended mid-flight — preserving the initial
    /// loading state of the ViewModel for snapshot-style tests.
    /// </summary>
    private static IArticleApiService CreateBlockingArticleApi()
    {
        var api = Substitute.For<IArticleApiService>();
        var tcs = new TaskCompletionSource<List<ArticleTreeDto>>();
        api.GetRootArticlesAsync().Returns(tcs.Task);
        api.GetArticleAsync(Arg.Any<Guid>()).Returns(tcs.Task.ContinueWith(_ => (ArticleDto?)null));
        return api;
    }

    // -------------------------------------------------------------------------
    // Branch: selected article → ArticleDetail
    // -------------------------------------------------------------------------

    [Fact]
    public void Cosmos_WhenArticleSelected_RendersArticleDetailBranch()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns(Guid.NewGuid());
        var vm = CreateViewModel(treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        Assert.Contains("article-detail-stub", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Your Chronicle Awaits", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Branch: no article selected → welcome / cosmos view
    // -------------------------------------------------------------------------

    [Fact]
    public void Cosmos_WhenNoArticleSelected_RendersWelcomeHero()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var vm = CreateViewModel(treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        Assert.Contains("Your Chronicle Awaits", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Stats cards
    // -------------------------------------------------------------------------

    [Fact]
    public void Cosmos_WhenStatsNull_DoesNotRenderStatCards()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var vm = CreateViewModel(articleApi: CreateBlockingArticleApi(), treeState: treeState);
        // InitializeAsync is suspended — VM.Stats remains null
        var cut = RenderWithViewModel(vm, treeState);

        // Stats section should not be present
        Assert.DoesNotContain("Total Articles", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_WhenStatsSet_RendersStatCards()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);

        var articleApi = Substitute.For<IArticleApiService>();
        var articleId = Guid.NewGuid();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = articleId, HasChildren = false },
        });
        articleApi.GetArticleAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Title = "Root",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        });

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        var vm = CreateViewModel(articleApi: articleApi, quoteService: quoteService, treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        // Trigger initialization so stats are populated
        await cut.InvokeAsync(() => vm.InitializeAsync(null));
        cut.Render();

        Assert.Contains("Total Articles", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Top-Level Topics", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Loading states
    // -------------------------------------------------------------------------

    [Fact]
    public void Cosmos_WhenIsLoadingRecentTrue_RendersLoadingIndicator()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var vm = CreateViewModel(articleApi: CreateBlockingArticleApi(), treeState: treeState);
        // InitializeAsync is suspended — IsLoadingRecent remains true
        var cut = RenderWithViewModel(vm, treeState);

        // The loading spinner for recent articles should be present
        Assert.Contains("mud-progress-circular", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cosmos_WhenLoadingQuoteTrue_RendersQuoteLoader()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);
        var vm = CreateViewModel(articleApi: CreateBlockingArticleApi(), treeState: treeState);
        // InitializeAsync is suspended — LoadingQuote remains true
        var cut = RenderWithViewModel(vm, treeState);

        Assert.Contains("mud-progress-circular", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Quote section
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cosmos_WhenQuoteLoaded_RendersQuoteContent()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns(new Quote { Content = "Roll for wisdom.", Author = "The DM" });

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());

        var vm = CreateViewModel(articleApi: articleApi, quoteService: quoteService, treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        await cut.InvokeAsync(() => vm.InitializeAsync(null));
        cut.Render();

        Assert.Contains("Roll for wisdom.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("The DM", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Recent articles
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cosmos_WhenRecentArticlesLoaded_RendersArticleTitles()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);

        var articleId = Guid.NewGuid();
        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = articleId, HasChildren = false },
        });
        articleApi.GetArticleAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Title = "The Dragon's Lair",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        });

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        var vm = CreateViewModel(articleApi: articleApi, quoteService: quoteService, treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        await cut.InvokeAsync(() => vm.InitializeAsync(null));
        cut.Render();

        Assert.Contains("The Dragon's Lair", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cosmos_WhenRecentArticleTitleEmpty_ShowsUntitledFallback()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);

        var articleId = Guid.NewGuid();
        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = articleId, HasChildren = false },
        });
        articleApi.GetArticleAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Title = string.Empty,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        });

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        var vm = CreateViewModel(articleApi: articleApi, quoteService: quoteService, treeState: treeState);
        var cut = RenderWithViewModel(vm, treeState);

        await cut.InvokeAsync(() => vm.InitializeAsync(null));
        cut.Render();

        Assert.Contains("Untitled", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Lifecycle wiring
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cosmos_OnInitializedAsync_CallsVmInitialize()
    {
        var treeState = Substitute.For<ITreeStateService>();
        treeState.SelectedArticleId.Returns((Guid?)null);

        var articleApi = Substitute.For<IArticleApiService>();
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        var vm = CreateViewModel(articleApi: articleApi, quoteService: quoteService, treeState: treeState);
        RenderWithViewModel(vm, treeState);

        // InitializeAsync is called on render; just verify loading completed
        await Task.Delay(50); // allow async to settle
        Assert.False(vm.IsLoadingRecent);
        Assert.False(vm.LoadingQuote);
    }

    // -------------------------------------------------------------------------
    // Stubs
    // -------------------------------------------------------------------------

    private sealed class ArticleDetailStub : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "article-detail-stub");
            builder.CloseElement();
        }
    }
}
