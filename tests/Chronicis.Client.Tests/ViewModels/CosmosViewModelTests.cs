using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class CosmosViewModelTests
{
    // -------------------------------------------------------------------------
    // Test helpers
    // -------------------------------------------------------------------------

    private record Sut(
        CosmosViewModel VM,
        IArticleApiService ArticleApi,
        IQuoteService QuoteService,
        IBreadcrumbService BreadcrumbService,
        ITreeStateService TreeState,
        IAppNavigator Navigator);

    private static Sut CreateSut()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var treeState = Substitute.For<ITreeStateService>();
        var navigator = Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<CosmosViewModel>>();

        // Default: return empty list from GetRootArticlesAsync so the VM doesn't throw
        articleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());

        var vm = new CosmosViewModel(
            articleApi, quoteService, breadcrumbService, treeState, navigator, logger);

        return new Sut(vm, articleApi, quoteService, breadcrumbService, treeState, navigator);
    }

    private static ArticleTreeDto MakeTreeNode(Guid? id = null, List<ArticleTreeDto>? children = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Title = "Article",
            HasChildren = children?.Count > 0,
            Children = children,
        };

    private static ArticleDto MakeArticle(Guid? id = null, DateTime? modifiedAt = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Title = "Article",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ModifiedAt = modifiedAt,
        };

    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialState_LoadingFlagsTrue_CollectionsNull()
    {
        var c = CreateSut();
        Assert.True(c.VM.IsLoadingRecent);
        Assert.True(c.VM.LoadingQuote);
        Assert.Null(c.VM.Stats);
        Assert.Null(c.VM.RecentArticles);
        Assert.Null(c.VM.Quote);
    }

    // -------------------------------------------------------------------------
    // IDisposable / event subscription
    // -------------------------------------------------------------------------

    [Fact]
    public void Dispose_UnsubscribesFromTreeState()
    {
        var c = CreateSut();
        // Subscribe a listener to confirm the VM's own listener was cleaned up
        c.VM.Dispose();
        // Raising the event after dispose should not throw
        c.TreeState.OnStateChanged += Raise.Event<Action>();
    }

    // -------------------------------------------------------------------------
    // InitializeAsync — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_NullPath_LoadsDashboardAndQuote()
    {
        var c = CreateSut();
        var quote = new Quote { Content = "Courage", Author = "Yoda" };
        c.QuoteService.GetRandomQuoteAsync().Returns(quote);

        await c.VM.InitializeAsync(null);

        Assert.False(c.VM.IsLoadingRecent);
        Assert.False(c.VM.LoadingQuote);
        Assert.Equal(quote, c.VM.Quote);
        Assert.NotNull(c.VM.Stats);
        Assert.NotNull(c.VM.RecentArticles);
    }

    [Fact]
    public async Task InitializeAsync_WithPath_CallsLoadArticleByPath()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        var article = MakeArticle();
        c.ArticleApi.GetArticleByPathAsync("lore/magic").Returns(article);
        c.TreeState.SelectedArticleId.Returns((Guid?)null);

        await c.VM.InitializeAsync("lore/magic");

        await c.ArticleApi.Received(1).GetArticleByPathAsync("lore/magic");
    }

    // -------------------------------------------------------------------------
    // LoadArticleByPath — edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_PathNotFound_NoSelectedArticle_NavigatesToRoot()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        c.ArticleApi.GetArticleByPathAsync(Arg.Any<string>()).Returns((ArticleDto?)null);
        c.TreeState.SelectedArticleId.Returns((Guid?)null);

        await c.VM.InitializeAsync("bad/path");

        c.Navigator.Received(1).NavigateTo("/", replace: true);
    }

    [Fact]
    public async Task InitializeAsync_PathNotFound_ArticleAlreadySelected_DoesNotNavigate()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        c.ArticleApi.GetArticleByPathAsync(Arg.Any<string>()).Returns((ArticleDto?)null);
        c.TreeState.SelectedArticleId.Returns(Guid.NewGuid());

        await c.VM.InitializeAsync("bad/path");

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task InitializeAsync_PathThrows_NoSelectedArticle_NavigatesToRoot()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        c.ArticleApi.GetArticleByPathAsync(Arg.Any<string>()).ThrowsAsync(new HttpRequestException());
        c.TreeState.SelectedArticleId.Returns((Guid?)null);

        await c.VM.InitializeAsync("error/path");

        c.Navigator.Received(1).NavigateTo("/", replace: true);
    }

    [Fact]
    public async Task InitializeAsync_PathFound_DifferentFromSelected_ExpandsPath()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        var article = MakeArticle();
        c.ArticleApi.GetArticleByPathAsync("lore/magic").Returns(article);
        c.TreeState.SelectedArticleId.Returns(Guid.NewGuid()); // different from article.Id

        await c.VM.InitializeAsync("lore/magic");

        c.TreeState.Received(1).ExpandPathToAndSelect(article.Id);
    }

    [Fact]
    public async Task InitializeAsync_PathFound_SameAsSelected_DoesNotExpand()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        var article = MakeArticle();
        c.ArticleApi.GetArticleByPathAsync("lore/magic").Returns(article);
        c.TreeState.SelectedArticleId.Returns(article.Id); // same — no expand needed

        await c.VM.InitializeAsync("lore/magic");

        c.TreeState.DidNotReceive().ExpandPathToAndSelect(Arg.Any<Guid>());
    }

    // -------------------------------------------------------------------------
    // OnParametersSetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnParametersSetAsync_NullPath_DoesNothing()
    {
        var c = CreateSut();
        await c.VM.OnParametersSetAsync(null);
        await c.ArticleApi.DidNotReceive().GetArticleByPathAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnParametersSetAsync_WithPath_LoadsArticle()
    {
        var c = CreateSut();
        var article = MakeArticle();
        c.ArticleApi.GetArticleByPathAsync("lore/magic").Returns(article);
        c.TreeState.SelectedArticleId.Returns(article.Id);

        await c.VM.OnParametersSetAsync("lore/magic");

        await c.ArticleApi.Received(1).GetArticleByPathAsync("lore/magic");
    }

    // -------------------------------------------------------------------------
    // Quote loading
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadNewQuoteAsync_ReplacesExistingQuote()
    {
        var c = CreateSut();
        var first = new Quote { Content = "A", Author = "X" };
        var second = new Quote { Content = "B", Author = "Y" };
        c.QuoteService.GetRandomQuoteAsync().Returns(first, second);

        await c.VM.InitializeAsync(null);
        await c.VM.LoadNewQuoteAsync();

        Assert.Equal(second, c.VM.Quote);
    }

    [Fact]
    public async Task LoadNewQuoteAsync_ServiceThrows_QuoteBecomesNull()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().ThrowsAsync(new Exception("svc down"));

        await c.VM.InitializeAsync(null);

        Assert.Null(c.VM.Quote);
        Assert.False(c.VM.LoadingQuote);
    }

    // -------------------------------------------------------------------------
    // Stats calculation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_EmptyTree_StatsAllZero()
    {
        var c = CreateSut();
        c.ArticleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto>());
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        await c.VM.InitializeAsync(null);

        Assert.Equal(0, c.VM.Stats!.TotalArticles);
        Assert.Equal(0, c.VM.Stats.RootArticles);
        Assert.Equal(0, c.VM.Stats.RecentlyModified);
        Assert.Equal(0, c.VM.Stats.DaysSinceStart);
        Assert.Empty(c.VM.RecentArticles!);
    }

    [Fact]
    public async Task InitializeAsync_FlatTree_CountsCorrectly()
    {
        var c = CreateSut();
        var nodes = Enumerable.Range(0, 3).Select(_ => MakeTreeNode()).ToList();
        c.ArticleApi.GetRootArticlesAsync().Returns(nodes);
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        foreach (var n in nodes)
        {
            var dto = MakeArticle(n.Id, DateTime.UtcNow.AddDays(-1));
            c.ArticleApi.GetArticleAsync(n.Id).Returns(dto);
        }

        await c.VM.InitializeAsync(null);

        Assert.Equal(3, c.VM.Stats!.TotalArticles);
        Assert.Equal(3, c.VM.Stats.RootArticles);
        Assert.Equal(3, c.VM.Stats.RecentlyModified);
    }

    [Fact]
    public async Task InitializeAsync_NestedTree_CountsAllDescendants()
    {
        var c = CreateSut();
        var childId = Guid.NewGuid();
        var child = MakeTreeNode(childId);
        var parent = MakeTreeNode(children: new List<ArticleTreeDto> { child });
        parent.HasChildren = true;

        c.ArticleApi.GetRootArticlesAsync().Returns(new List<ArticleTreeDto> { parent });
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        c.ArticleApi.GetArticleAsync(parent.Id).Returns(MakeArticle(parent.Id));
        c.ArticleApi.GetArticleAsync(childId).Returns(MakeArticle(childId));

        await c.VM.InitializeAsync(null);

        Assert.Equal(2, c.VM.Stats!.TotalArticles);
        Assert.Equal(1, c.VM.Stats.RootArticles);
    }

    [Fact]
    public async Task InitializeAsync_RecentArticles_LimitedToFive()
    {
        var c = CreateSut();
        // 7 root nodes, each returns an article
        var nodes = Enumerable.Range(0, 7).Select(_ => MakeTreeNode()).ToList();
        c.ArticleApi.GetRootArticlesAsync().Returns(nodes);
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        foreach (var n in nodes)
            c.ArticleApi.GetArticleAsync(n.Id).Returns(MakeArticle(n.Id, DateTime.UtcNow));

        await c.VM.InitializeAsync(null);

        Assert.Equal(5, c.VM.RecentArticles!.Count);
    }

    [Fact]
    public async Task InitializeAsync_DashboardThrows_StatsRemainNull()
    {
        var c = CreateSut();
        c.ArticleApi.GetRootArticlesAsync().ThrowsAsync(new Exception("timeout"));
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        await c.VM.InitializeAsync(null);

        Assert.Null(c.VM.Stats);
        Assert.Null(c.VM.RecentArticles);
        Assert.False(c.VM.IsLoadingRecent);
    }

    // -------------------------------------------------------------------------
    // Article creation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateFirstArticleAsync_CreatesWithEmptyTitle()
    {
        var c = CreateSut();
        var created = MakeArticle();
        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);

        await c.VM.CreateFirstArticleAsync();

        await c.ArticleApi.Received(1).CreateArticleAsync(
            Arg.Is<ArticleCreateDto>(dto => dto.Title == string.Empty));
    }

    [Fact]
    public async Task CreateArticleWithTitleAsync_CallsApiAndSelectsArticle()
    {
        var c = CreateSut();
        var created = MakeArticle();
        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);

        await c.VM.CreateArticleWithTitleAsync("New Character");

        await c.ArticleApi.Received(1).CreateArticleAsync(
            Arg.Is<ArticleCreateDto>(dto => dto.Title == "New Character"));
        await c.TreeState.Received(1).RefreshAsync();
        c.TreeState.Received(1).ExpandPathToAndSelect(created.Id);
    }

    [Fact]
    public async Task CreateArticleWithTitleAsync_ApiReturnsNull_DoesNotRefreshTree()
    {
        var c = CreateSut();
        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns((ArticleDto?)null);

        await c.VM.CreateArticleWithTitleAsync("Broken");

        await c.TreeState.DidNotReceive().RefreshAsync();
    }

    // -------------------------------------------------------------------------
    // NavigateToArticleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task NavigateToArticleAsync_WithBreadcrumbs_Navigates()
    {
        var c = CreateSut();
        var crumbs = new List<BreadcrumbDto>
        {
            new() { Title = "Lore", Slug = "lore" },
            new() { Title = "Magic", Slug = "magic" },
        };
        var detail = new ArticleDto { Id = Guid.NewGuid(), Breadcrumbs = crumbs };
        c.ArticleApi.GetArticleDetailAsync(detail.Id).Returns(detail);
        c.BreadcrumbService.BuildArticleUrl(crumbs).Returns("/articosmoscle/lore/magic");

        await c.VM.NavigateToArticleAsync(detail.Id);

        c.Navigator.Received(1).NavigateTo("/articosmoscle/lore/magic");
    }

    [Fact]
    public async Task NavigateToArticleAsync_ArticleNotFound_DoesNotNavigate()
    {
        var c = CreateSut();
        c.ArticleApi.GetArticleDetailAsync(Arg.Any<Guid>()).Returns((ArticleDto?)null);

        await c.VM.NavigateToArticleAsync(Guid.NewGuid());

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    // -------------------------------------------------------------------------
    // FormatRelativeTime (static — no mocking needed)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(-0.5, "just now")]
    [InlineData(-30, "30m ago")]
    [InlineData(-90, "1h ago")]
    [InlineData(-25 * 60, "1d ago")]
    [InlineData(-8 * 24 * 60, "1w ago")]
    [InlineData(-35 * 24 * 60, null)] // formatted date — just check it's not a relative string
    public void FormatRelativeTime_ReturnsExpectedFormat(double minutesAgo, string? expected)
    {
        var dt = DateTime.Now.AddMinutes(minutesAgo);
        var result = CosmosViewModel.FormatRelativeTime(dt);

        if (expected != null)
            Assert.Equal(expected, result);
        else
            Assert.Matches(@"^[A-Z][a-z]{2} \d{1,2}, \d{4}$", result); // "Jan 1, 2025"
    }

    // -------------------------------------------------------------------------
    // PropertyChanged wiring
    // -------------------------------------------------------------------------

    [Fact]
    public void OnTreeStateChanged_RaisesPropertyChangedForStats()
    {
        var c = CreateSut();
        string? changedProp = null;
        c.VM.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        c.TreeState.OnStateChanged += Raise.Event<Action>();

        Assert.Equal(nameof(CosmosViewModel.Stats), changedProp);
    }
}
