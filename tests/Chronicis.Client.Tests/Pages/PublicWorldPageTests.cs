using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Render-only tests for the PublicWorldPage shell.
/// Business logic is fully covered by PublicWorldPageViewModelTests.
/// These tests verify that the page renders the correct UI branches based on ViewModel state.
/// </summary>
public class PublicWorldPageTests : MudBlazorTestContext
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private PublicWorldPageViewModel CreateViewModel(
        IPublicApiService? publicApi = null,
        IAppNavigator? navigator = null)
    {
        publicApi ??= Substitute.For<IPublicApiService>();
        navigator ??= Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<PublicWorldPageViewModel>>();
        return new PublicWorldPageViewModel(publicApi, navigator, logger);
    }

    private IRenderedComponent<PublicWorldPage> RenderWithViewModel(
        PublicWorldPageViewModel vm,
        string publicSlug = "my-world",
        string? articlePath = null)
    {
        Services.AddSingleton(vm);
        Services.AddSingleton(Substitute.For<ILogger<PublicWorldPage>>());
        Services.AddSingleton(Substitute.For<IMarkdownService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var url = articlePath is null
            ? $"http://localhost/w/{publicSlug}"
            : $"http://localhost/w/{publicSlug}/{articlePath}";
        nav.NavigateTo(url);

        return RenderComponent<PublicWorldPage>(p =>
        {
            p.Add(x => x.PublicSlug, publicSlug);
            if (articlePath is not null)
                p.Add(x => x.ArticlePath, articlePath);
        });
    }

    // -------------------------------------------------------------------------
    // Loading state
    // -------------------------------------------------------------------------

    [Fact]
    public void PublicWorldPage_WhenIsLoadingTrue_RendersLoadingSpinner()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        var tcs = new TaskCompletionSource<WorldDetailDto?>();
        publicApi.GetPublicWorldAsync(Arg.Any<string>()).Returns(tcs.Task);

        var vm = CreateViewModel(publicApi: publicApi);

        // Don't await LoadWorldAsync — IsLoading remains true
        var cut = RenderWithViewModel(vm);

        Assert.Contains("Loading world", cut.Markup, StringComparison.OrdinalIgnoreCase);

        tcs.SetResult(null);
    }

    // -------------------------------------------------------------------------
    // World not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublicWorldPage_WhenWorldNull_RendersNotFoundState()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        // Simulate VM state after load: world = null, not loading
        await cut.InvokeAsync(async () =>
        {
            await vm.LoadWorldAsync("missing", null);
        });

        cut.Render();
        Assert.Contains("World Not Found", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // World landing (no article path)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublicWorldPage_WhenWorldLoadedAndNoArticlePath_RendersWorldName()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("test-world").Returns(new WorldDetailDto
        {
            Name = "Test World",
            OwnerName = "DM Dave",
            Description = "A great world"
        });
        publicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());

        var vm = CreateViewModel(publicApi: publicApi);
        var cut = RenderWithViewModel(vm, "test-world");

        await cut.InvokeAsync(async () => await vm.LoadWorldAsync("test-world", null));
        cut.Render();

        Assert.Contains("Test World", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicWorldPage_WhenWorldHasNoArticles_RendersEmptyTreeMessage()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("empty-world").Returns(new WorldDetailDto { Name = "Empty" });
        publicApi.GetPublicArticleTreeAsync("empty-world").Returns(new List<ArticleTreeDto>());

        var vm = CreateViewModel(publicApi: publicApi);
        var cut = RenderWithViewModel(vm, "empty-world");

        await cut.InvokeAsync(async () => await vm.LoadWorldAsync("empty-world", null));
        cut.Render();

        Assert.Contains("No public articles", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Article not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublicWorldPage_WhenArticlePathSetButArticleNull_RendersArticleNotFound()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("test-world").Returns(new WorldDetailDto { Name = "Test World" });
        publicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        publicApi.GetPublicArticleAsync("test-world", "missing/path").Returns((ArticleDto?)null);

        var vm = CreateViewModel(publicApi: publicApi);
        var cut = RenderWithViewModel(vm, "test-world", "missing/path");

        await cut.InvokeAsync(async () => await vm.LoadWorldAsync("test-world", "missing/path"));
        cut.Render();

        Assert.Contains("Article Not Found", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Article loaded
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublicWorldPage_WhenArticleLoaded_RendersArticleTitle()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("test-world").Returns(new WorldDetailDto { Name = "Test World" });
        publicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        publicApi.GetPublicArticleAsync("test-world", "lore/magic").Returns(new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Magic",
            Body = "<p>Content</p>"
        });

        var markdownService = Substitute.For<IMarkdownService>();
        markdownService.ToHtml(Arg.Any<string>()).Returns("<p>Content</p>");

        var vm = CreateViewModel(publicApi: publicApi);

        Services.AddSingleton(vm);
        Services.AddSingleton(Substitute.For<ILogger<PublicWorldPage>>());
        Services.AddSingleton(markdownService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/w/test-world/lore/magic");

        var cut = RenderComponent<PublicWorldPage>(p =>
        {
            p.Add(x => x.PublicSlug, "test-world");
            p.Add(x => x.ArticlePath, "lore/magic");
        });

        await cut.InvokeAsync(async () => await vm.LoadWorldAsync("test-world", "lore/magic"));
        cut.Render();

        Assert.Contains("Magic", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicWorldPage_WhenArticleTreeHasItems_RendersSidebarTreeItems()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("tree-world").Returns(new WorldDetailDto { Name = "Tree World" });
        publicApi.GetPublicArticleTreeAsync("tree-world").Returns(new List<ArticleTreeDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Lore", Slug = "lore", Children = new List<ArticleTreeDto>() }
        });

        var vm = CreateViewModel(publicApi: publicApi);
        var cut = RenderWithViewModel(vm, "tree-world");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("public-article-tree", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Lore", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void PublicWorldPage_WhenArticleIsLoading_RendersSkeletonState()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        publicApi.GetPublicWorldAsync("test-world").Returns(new WorldDetailDto { Name = "Test World" });
        publicApi.GetPublicArticleTreeAsync("test-world").Returns(new List<ArticleTreeDto>());
        var articleTcs = new TaskCompletionSource<ArticleDto?>();
        publicApi.GetPublicArticleAsync("test-world", "lore/magic").Returns(articleTcs.Task);

        var vm = CreateViewModel(publicApi: publicApi);
        var cut = RenderWithViewModel(vm, "test-world", "lore/magic");

        cut.WaitForAssertion(() =>
            Assert.Contains("mud-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase));

        articleTcs.SetResult(null);
    }
}
