using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class PublicWorldPageViewModelTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private record Sut(
        PublicWorldPageViewModel VM,
        IPublicApiService PublicApi,
        IAppNavigator Navigator);

    private static Sut CreateSut()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        var navigator = Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<PublicWorldPageViewModel>>();

        var vm = new PublicWorldPageViewModel(publicApi, navigator, logger);
        return new Sut(vm, publicApi, navigator);
    }

    private static WorldDetailDto MakeWorld(string slug = "my-world", string? publicSlug = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "My World",
            Slug = slug,
            PublicSlug = publicSlug,
            OwnerName = "Dave"
        };

    private static ArticleDto MakeArticle(Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Title = "Test Article",
            Body = "Some body text",
            Type = ArticleType.WikiArticle,
            Breadcrumbs = new List<BreadcrumbDto>(),
        };

    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialState_LoadingTrueWorldNull()
    {
        var c = CreateSut();
        Assert.True(c.VM.IsLoading);
        Assert.Null(c.VM.World);
        Assert.Empty(c.VM.ArticleTree);
        Assert.Null(c.VM.CurrentArticle);
        Assert.False(c.VM.IsLoadingArticle);
        Assert.False(c.VM.WikiLinksInitialized);
    }

    // -------------------------------------------------------------------------
    // LoadWorldAsync — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadWorldAsync_WorldExists_SetsWorldAndTree()
    {
        var c = CreateSut();
        var world = MakeWorld();
        var tree = new List<ArticleTreeDto> { new() { Id = Guid.NewGuid(), Title = "Root" } };
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(tree);

        await c.VM.LoadWorldAsync("my-world", null);

        Assert.Equal(world, c.VM.World);
        Assert.Equal(tree, c.VM.ArticleTree);
        Assert.Null(c.VM.CurrentArticle);
        Assert.False(c.VM.IsLoading);
    }

    [Fact]
    public async Task LoadWorldAsync_WorldNotFound_WorldRemainsNull()
    {
        var c = CreateSut();
        c.PublicApi.GetPublicWorldAsync("unknown").Returns((WorldDetailDto?)null);

        await c.VM.LoadWorldAsync("unknown", null);

        Assert.Null(c.VM.World);
        Assert.False(c.VM.IsLoading);
        await c.PublicApi.DidNotReceive().GetPublicArticleTreeAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task LoadWorldAsync_WithArticlePath_LoadsArticle()
    {
        var c = CreateSut();
        var world = MakeWorld();
        var article = MakeArticle();
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());
        c.PublicApi.GetPublicArticleAsync("my-world", "lore/magic").Returns(article);

        await c.VM.LoadWorldAsync("my-world", "lore/magic");

        Assert.Equal(article, c.VM.CurrentArticle);
        Assert.False(c.VM.IsLoadingArticle);
    }

    [Fact]
    public async Task LoadWorldAsync_ResetsWikiLinksInitialized()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());

        // First load with an article that sets WikiLinksInitialized
        var article = MakeArticle();
        c.PublicApi.GetPublicArticleAsync("my-world", "p1").Returns(article);
        await c.VM.LoadWorldAsync("my-world", "p1");

        // Simulate JS init having run
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("initializePublicWikiLinks", Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));
        await c.VM.InitializeWikiLinksAsync(jsRuntime);

        // Navigate to another article — flag must reset
        c.PublicApi.GetPublicArticleAsync("my-world", "p2").Returns(MakeArticle());
        await c.VM.LoadWorldAsync("my-world", "p2");

        Assert.False(c.VM.WikiLinksInitialized);
    }

    [Fact]
    public async Task LoadWorldAsync_NullArticlePath_ClearsCurrentArticle()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());
        var article = MakeArticle();
        c.PublicApi.GetPublicArticleAsync("my-world", "lore").Returns(article);

        // First: load with article
        await c.VM.LoadWorldAsync("my-world", "lore");
        Assert.NotNull(c.VM.CurrentArticle);

        // Then: navigate to world landing (null path)
        await c.VM.LoadWorldAsync("my-world", null);
        Assert.Null(c.VM.CurrentArticle);
    }

    // -------------------------------------------------------------------------
    // InitializeWikiLinksAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeWikiLinksAsync_InvokesJsAndSetsFlag()
    {
        var c = CreateSut();
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("initializePublicWikiLinks", Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));

        await c.VM.InitializeWikiLinksAsync(jsRuntime);

        Assert.True(c.VM.WikiLinksInitialized);
        await jsRuntime.Received(1).InvokeAsync<object>(
            "initializePublicWikiLinks", Arg.Any<object[]>());
    }

    [Fact]
    public async Task InitializeWikiLinksAsync_AlreadyInitialized_DoesNotInvokeJs()
    {
        var c = CreateSut();
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("initializePublicWikiLinks", Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));

        await c.VM.InitializeWikiLinksAsync(jsRuntime);
        await c.VM.InitializeWikiLinksAsync(jsRuntime);

        await jsRuntime.Received(1).InvokeAsync<object>(
            "initializePublicWikiLinks", Arg.Any<object[]>());
    }

    [Fact]
    public async Task InitializeWikiLinksAsync_JsThrows_FlagRemainsUnset()
    {
        var c = CreateSut();
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("initializePublicWikiLinks", Arg.Any<object[]>())
            .ThrowsAsync(new JSException("JS unavailable"));

        await c.VM.InitializeWikiLinksAsync(jsRuntime); // must not throw

        Assert.False(c.VM.WikiLinksInitialized);
    }

    // -------------------------------------------------------------------------
    // OnPublicWikiLinkClicked
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnPublicWikiLinkClicked_ValidId_ResolvesAndNavigates()
    {
        var c = CreateSut();
        var world = MakeWorld("test-world");
        c.PublicApi.GetPublicWorldAsync("test-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        await c.VM.LoadWorldAsync("test-world", null);

        var articleId = Guid.NewGuid();
        c.PublicApi.ResolvePublicArticlePathAsync("test-world", articleId).Returns("lore/magic");

        await c.VM.OnPublicWikiLinkClicked(articleId.ToString());

        c.Navigator.Received(1).NavigateTo("/w/test-world/lore/magic");
    }

    [Fact]
    public async Task OnPublicWikiLinkClicked_PrefersPublicSlug_WhenAvailable()
    {
        var c = CreateSut();
        var world = MakeWorld("internal-world", "public-world");
        c.PublicApi.GetPublicWorldAsync("public-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("public-world").Returns(new List<ArticleTreeDto>());
        await c.VM.LoadWorldAsync("public-world", null);

        var articleId = Guid.NewGuid();
        c.PublicApi.ResolvePublicArticlePathAsync("public-world", articleId).Returns("lore/magic");

        await c.VM.OnPublicWikiLinkClicked(articleId.ToString());

        await c.PublicApi.Received(1).ResolvePublicArticlePathAsync("public-world", articleId);
        c.Navigator.Received(1).NavigateTo("/w/public-world/lore/magic");
    }

    [Fact]
    public async Task OnPublicWikiLinkClicked_InvalidGuid_DoesNotNavigate()
    {
        var c = CreateSut();
        await c.VM.OnPublicWikiLinkClicked("not-a-guid");
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task OnPublicWikiLinkClicked_NoWorldLoaded_DoesNotNavigate()
    {
        var c = CreateSut();
        // World is null / not loaded → slug is empty
        await c.VM.OnPublicWikiLinkClicked(Guid.NewGuid().ToString());
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task OnPublicWikiLinkClicked_PathNotResolved_DoesNotNavigate()
    {
        var c = CreateSut();
        var world = MakeWorld("test-world");
        c.PublicApi.GetPublicWorldAsync("test-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        await c.VM.LoadWorldAsync("test-world", null);

        var articleId = Guid.NewGuid();
        c.PublicApi.ResolvePublicArticlePathAsync("test-world", articleId).Returns((string?)null);

        await c.VM.OnPublicWikiLinkClicked(articleId.ToString());

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task OnPublicWikiLinkClicked_ApiThrows_DoesNotPropagate()
    {
        var c = CreateSut();
        var world = MakeWorld("test-world");
        c.PublicApi.GetPublicWorldAsync("test-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        await c.VM.LoadWorldAsync("test-world", null);

        var articleId = Guid.NewGuid();
        c.PublicApi.ResolvePublicArticlePathAsync(Arg.Any<string>(), Arg.Any<Guid>())
            .ThrowsAsync(new HttpRequestException("down"));

        // Must not throw
        await c.VM.OnPublicWikiLinkClicked(articleId.ToString());
    }

    // -------------------------------------------------------------------------
    // NavigateToArticle
    // -------------------------------------------------------------------------

    [Fact]
    public void NavigateToArticle_WithPath_NavigatesToArticleUrl()
    {
        var c = CreateSut();
        c.VM.NavigateToArticle("my-world", "lore/magic");
        c.Navigator.Received(1).NavigateTo("/w/my-world/lore/magic");
    }

    [Fact]
    public void NavigateToArticle_EmptyPath_NavigatesToWorldRoot()
    {
        var c = CreateSut();
        c.VM.NavigateToArticle("my-world", string.Empty);
        c.Navigator.Received(1).NavigateTo("/w/my-world");
    }

    // -------------------------------------------------------------------------
    // GetBreadcrumbItems
    // -------------------------------------------------------------------------

    [Fact]
    public void GetBreadcrumbItems_NoCurrentArticle_ReturnsEmptyList()
    {
        var c = CreateSut();
        var result = c.VM.GetBreadcrumbItems("my-world");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBreadcrumbItems_WithArticleAndNoBreadcrumbs_ReturnsEmptyList()
    {
        var c = CreateSut();
        var world = MakeWorld();
        var article = MakeArticle();
        article.Breadcrumbs = new List<BreadcrumbDto>();
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());
        c.PublicApi.GetPublicArticleAsync("my-world", "lore").Returns(article);
        await c.VM.LoadWorldAsync("my-world", "lore");

        var result = c.VM.GetBreadcrumbItems("my-world");

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetPageTitle
    // -------------------------------------------------------------------------

    [Fact]
    public void GetPageTitle_NoWorldLoaded_ReturnsDefaultTitle()
    {
        var c = CreateSut();
        Assert.Equal("World — Chronicis", c.VM.GetPageTitle());
    }

    [Fact]
    public async Task GetPageTitle_WorldLoaded_IncludesWorldName()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());
        await c.VM.LoadWorldAsync("my-world", null);

        Assert.Equal("My World — Chronicis", c.VM.GetPageTitle());
    }

    [Fact]
    public void GetRenderedArticleBodyHtml_NoCurrentArticle_ReturnsEmptyString()
    {
        var c = CreateSut();
        var markdownService = Substitute.For<IMarkdownService>();

        var rendered = c.VM.GetRenderedArticleBodyHtml(markdownService);

        Assert.Equal(string.Empty, rendered);
    }

    [Fact]
    public async Task GetRenderedArticleBodyHtml_RewritesInlineImageReferencesToPublicEndpoint()
    {
        var c = CreateSut();
        var world = MakeWorld();
        var imageId = Guid.NewGuid();
        var article = MakeArticle();
        article.Body = $"<p>Text</p><img src=\"chronicis-image:{imageId}\" alt=\"img\" />";

        c.PublicApi.GetPublicWorldAsync("my-world").Returns(world);
        c.PublicApi.GetPublicArticleTreeAsync("my-world").Returns(new List<ArticleTreeDto>());
        c.PublicApi.GetPublicArticleAsync("my-world", "lore").Returns(article);

        await c.VM.LoadWorldAsync("my-world", "lore");

        var markdownService = Substitute.For<IMarkdownService>();
        markdownService.EnsureHtml(Arg.Any<string>())
            .Returns(args => args.Arg<string>());

        var rendered = c.VM.GetRenderedArticleBodyHtml(markdownService);

        Assert.Contains($"/api/public/documents/{imageId}", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("chronicis-image:", rendered, StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------
    // GetArticleTypeLabel (static)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(ArticleType.WikiArticle, "Wiki Article")]
    [InlineData(ArticleType.Character, "Character")]
    [InlineData(ArticleType.CharacterNote, "Character Note")]
    [InlineData(ArticleType.Session, "Session")]
    [InlineData(ArticleType.SessionNote, "Session Note")]
    [InlineData(ArticleType.Legacy, "Article")]
    public void GetArticleTypeLabel_KnownTypes_ReturnExpectedLabel(ArticleType type, string expected)
    {
        Assert.Equal(expected, PublicWorldPageViewModel.GetArticleTypeLabel(type));
    }

    [Fact]
    public void GetArticleTypeLabel_UnknownType_ReturnsArticle()
    {
        var unknownType = (ArticleType)999;
        Assert.Equal("Article", PublicWorldPageViewModel.GetArticleTypeLabel(unknownType));
    }

    // -------------------------------------------------------------------------
    // IAsyncDisposable
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DisposeAsync_CompletesWithoutThrowing()
    {
        var c = CreateSut();
        await c.VM.DisposeAsync(); // no JS ref allocated — should be a no-op
    }

    [Fact]
    public async Task DisposeAsync_AfterJsInit_DisposesReference()
    {
        var c = CreateSut();
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("initializePublicWikiLinks", Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));

        await c.VM.InitializeWikiLinksAsync(jsRuntime);
        await c.VM.DisposeAsync(); // must not throw
    }
}
