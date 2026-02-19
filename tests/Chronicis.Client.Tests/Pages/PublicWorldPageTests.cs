using System.Reflection;
using Bunit;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class PublicWorldPageTests : MudBlazorTestContext
{
    [Fact]
    public void PublicWorldPage_NavigateToArticle_EmptyPath_NavigatesToWorldRoot()
    {
        var rendered = CreateRenderedSut("my-world");

        InvokePrivate(rendered.Instance, "NavigateToArticle", string.Empty);

        Assert.EndsWith("/w/my-world", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicWorldPage_NavigateToArticle_Path_NavigatesToArticle()
    {
        var rendered = CreateRenderedSut("my-world");

        InvokePrivate(rendered.Instance, "NavigateToArticle", "lore/entry");

        Assert.EndsWith("/w/my-world/lore/entry", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicWorldPage_OnPublicWikiLinkClicked_InvalidGuid_DoesNothing()
    {
        var rendered = CreateRenderedSut("my-world");
        var before = rendered.Navigation.Uri;

        await rendered.Cut.InvokeAsync(() => rendered.Instance.OnPublicWikiLinkClicked("not-a-guid"));

        await rendered.PublicApi.DidNotReceive().ResolvePublicArticlePathAsync(Arg.Any<string>(), Arg.Any<Guid>());
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task PublicWorldPage_OnPublicWikiLinkClicked_ResolvedPath_Navigates()
    {
        var rendered = CreateRenderedSut("my-world");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.ResolvePublicArticlePathAsync("my-world", articleId).Returns("path/to/article");

        await rendered.Cut.InvokeAsync(() => rendered.Instance.OnPublicWikiLinkClicked(articleId.ToString()));

        Assert.EndsWith("/w/my-world/path/to/article", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicWorldPage_OnPublicWikiLinkClicked_UnresolvedPath_DoesNotNavigate()
    {
        var rendered = CreateRenderedSut("my-world");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.ResolvePublicArticlePathAsync("my-world", articleId).Returns((string?)null);
        var before = rendered.Navigation.Uri;

        await rendered.Cut.InvokeAsync(() => rendered.Instance.OnPublicWikiLinkClicked(articleId.ToString()));

        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task PublicWorldPage_OnPublicWikiLinkClicked_WhenResolveThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut("my-world");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.ResolvePublicArticlePathAsync("my-world", articleId)
            .Returns(Task.FromException<string?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => rendered.Cut.InvokeAsync(() => rendered.Instance.OnPublicWikiLinkClicked(articleId.ToString())));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PublicWorldPage_LoadWorldAsync_WhenWorldMissing_DoesNotLoadTreeOrArticle()
    {
        var rendered = CreateRenderedSut("missing", "x/y");
        rendered.PublicApi.GetPublicWorldAsync("missing").Returns((WorldDetailDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        await rendered.PublicApi.DidNotReceive().GetPublicArticleTreeAsync(Arg.Any<string>());
        await rendered.PublicApi.DidNotReceive().GetPublicArticleAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task PublicWorldPage_LoadWorldAsync_WithArticlePath_LoadsWorldTreeAndArticle()
    {
        var rendered = CreateRenderedSut("world", "lore/entry");
        rendered.PublicApi.GetPublicWorldAsync("world").Returns(new WorldDetailDto { Name = "World" });
        rendered.PublicApi.GetPublicArticleTreeAsync("world").Returns(new List<ArticleTreeDto> { new() { Id = Guid.NewGuid() } });
        rendered.PublicApi.GetPublicArticleAsync("world", "lore/entry").Returns(new ArticleDto { Id = Guid.NewGuid(), Title = "Entry" });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        await rendered.PublicApi.Received().GetPublicWorldAsync("world");
        await rendered.PublicApi.Received().GetPublicArticleTreeAsync("world");
        await rendered.PublicApi.Received().GetPublicArticleAsync("world", "lore/entry");
    }

    [Fact]
    public async Task PublicWorldPage_LoadWorldAsync_WhenNoArticlePath_LoadsWorldAndTreeOnly()
    {
        var rendered = CreateRenderedSut("world");
        rendered.PublicApi.GetPublicWorldAsync("world").Returns(new WorldDetailDto { Name = "World" });
        rendered.PublicApi.GetPublicArticleTreeAsync("world").Returns(new List<ArticleTreeDto> { new() { Id = Guid.NewGuid() } });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        await rendered.PublicApi.Received().GetPublicWorldAsync("world");
        await rendered.PublicApi.Received().GetPublicArticleTreeAsync("world");
        await rendered.PublicApi.DidNotReceive().GetPublicArticleAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task PublicWorldPage_LoadArticleAsync_WhenPathEmpty_DoesNotCallApi()
    {
        var rendered = CreateRenderedSut("world");

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadArticleAsync");

        await rendered.PublicApi.DidNotReceive().GetPublicArticleAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task PublicWorldPage_OnAfterRenderAsync_WhenNoArticleBody_DoesNotInitializeWikiLinks()
    {
        var rendered = CreateRenderedSut("world");
        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto { Id = Guid.NewGuid(), Body = string.Empty });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnAfterRenderAsync", true);

        await rendered.JsRuntime.DidNotReceive().InvokeVoidAsync("initializePublicWikiLinks", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task PublicWorldPage_OnAfterRenderAsync_WhenBodyPresent_InitializesWikiLinksOnce()
    {
        var rendered = CreateRenderedSut("world");
        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto { Id = Guid.NewGuid(), Body = "content" });
        SetPrivateField(rendered.Instance, "_wikiLinksInitialized", false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnAfterRenderAsync", true);
        await InvokePrivateOnRendererAsync(rendered.Cut, "OnAfterRenderAsync", false);

        await rendered.JsRuntime.Received(1).InvokeVoidAsync("initializePublicWikiLinks", Arg.Any<object?[]>());
        Assert.True(GetPrivateField<bool>(rendered.Instance, "_wikiLinksInitialized"));
    }

    [Fact]
    public async Task PublicWorldPage_OnAfterRenderAsync_WhenJsThrows_SwallowsException()
    {
        var rendered = CreateRenderedSut("world");
        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto { Id = Guid.NewGuid(), Body = "content" });
        rendered.JsRuntime
            .When(x => x.InvokeVoidAsync("initializePublicWikiLinks", Arg.Any<object?[]>()))
            .Do(_ => throw new Exception("js failed"));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "OnAfterRenderAsync", true));

        Assert.Null(ex);
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_wikiLinksInitialized"));
    }

    [Fact]
    public async Task PublicWorldPage_GetBreadcrumbItems_HandlesVirtualAndRealCrumbs()
    {
        var rendered = CreateRenderedSut("pub");
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var real1 = Guid.NewGuid();
        var real2 = Guid.NewGuid();

        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto
        {
            Id = real2,
            CampaignId = campaignId,
            ArcId = arcId,
            Breadcrumbs =
            [
                new BreadcrumbDto { Id = worldId, Title = "World", Slug = "world", IsWorld = true },
                new BreadcrumbDto { Id = Guid.Empty, Title = "Wiki", Slug = "wiki", IsWorld = false },
                new BreadcrumbDto { Id = campaignId, Title = "Campaign", Slug = "camp", IsWorld = false },
                new BreadcrumbDto { Id = arcId, Title = "Arc", Slug = "arc", IsWorld = false },
                new BreadcrumbDto { Id = real1, Title = "Chapter", Slug = "chapter", IsWorld = false },
                new BreadcrumbDto { Id = real2, Title = "Entry", Slug = "entry", IsWorld = false }
            ]
        });

        var items = await InvokePrivateWithResultAsync<List<BreadcrumbItem>>(rendered.Instance, "GetBreadcrumbItems");

        Assert.Equal(6, items.Count);
        Assert.Equal("/w/pub", items[0].Href);
        Assert.True(items[1].Disabled);
        Assert.True(items[2].Disabled);
        Assert.True(items[3].Disabled);
        Assert.Equal("/w/pub/chapter", items[4].Href);
        Assert.True(items[5].Disabled);
    }

    [Fact]
    public async Task PublicWorldPage_GetBreadcrumbItems_WhenCurrentArticleNull_ReturnsEmpty()
    {
        var rendered = CreateRenderedSut("pub");
        SetPrivateField(rendered.Instance, "_currentArticle", null);

        var items = await InvokePrivateWithResultAsync<List<BreadcrumbItem>>(rendered.Instance, "GetBreadcrumbItems");

        Assert.Empty(items);
    }

    [Fact]
    public async Task PublicWorldPage_GetBreadcrumbItems_WhenBreadcrumbsNull_ReturnsEmpty()
    {
        var rendered = CreateRenderedSut("pub");
        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto { Id = Guid.NewGuid(), Breadcrumbs = null! });

        var items = await InvokePrivateWithResultAsync<List<BreadcrumbItem>>(rendered.Instance, "GetBreadcrumbItems");

        Assert.Empty(items);
    }

    [Fact]
    public void PublicWorldPage_Render_WhenWorldMissing_ShowsNotFoundState()
    {
        var rendered = CreateRenderedSut("pub");
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_world", null);

        rendered.Cut.Render();

        Assert.Contains("World Not Found", rendered.Cut.Markup);
    }

    [Fact]
    public void PublicWorldPage_Render_LandingWithoutDescriptionOrArticles_ShowsEmptySidebarMessage()
    {
        var rendered = CreateRenderedSut("pub");
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner", Description = string.Empty });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto>());

        rendered.Cut.SetParametersAndRender(parameters => parameters.Add(x => x.PublicSlug, "pub"));
        rendered.Cut.WaitForAssertion(() => Assert.Contains("No public articles in this world yet.", rendered.Cut.Markup));
        Assert.Contains("Created by Owner", rendered.Cut.Markup);
        Assert.DoesNotContain("Select an article from the sidebar to start reading.", rendered.Cut.Markup);
    }

    [Fact]
    public void PublicWorldPage_Render_LandingWithDescriptionAndArticles_ShowsDescriptionAndPrompt()
    {
        var rendered = CreateRenderedSut("pub");
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner", Description = "World description" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = Guid.NewGuid(), Title = "Entry" } });

        rendered.Cut.SetParametersAndRender(parameters => parameters.Add(x => x.PublicSlug, "pub"));
        rendered.Cut.WaitForAssertion(() => Assert.Contains("World description", rendered.Cut.Markup));
        Assert.Contains("Select an article from the sidebar to start reading.", rendered.Cut.Markup);
    }

    [Fact]
    public void PublicWorldPage_Render_ArticleNotFound_BackButtonNavigatesToWorldRoot()
    {
        var rendered = CreateRenderedSut("pub", "missing/path");
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = Guid.NewGuid(), Title = "Entry" } });
        rendered.PublicApi.GetPublicArticleAsync("pub", "missing/path").Returns((ArticleDto?)null);

        rendered.Cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(x => x.PublicSlug, "pub");
            parameters.Add(x => x.ArticlePath, "missing/path");
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("Article Not Found", rendered.Cut.Markup));

        var backButton = rendered.Cut.FindAll("button").Single(x => x.TextContent.Contains("Back to World Overview", StringComparison.Ordinal));
        backButton.Click();

        Assert.EndsWith("/w/pub", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicWorldPage_Render_ArticleView_WithMetadataAndEmptyBody_ShowsSummaryAndEmptyMessage()
    {
        var rendered = CreateRenderedSut("pub", "entry/path");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = articleId, Title = "Entry" } });
        rendered.PublicApi.GetPublicArticleAsync("pub", "entry/path").Returns(new ArticleDto
        {
            Id = articleId,
            Title = "Entry",
            Type = ArticleType.WikiArticle,
            IconEmoji = "book",
            AISummary = "Summary text",
            Body = string.Empty,
            ModifiedAt = DateTime.UtcNow,
            Breadcrumbs = [new BreadcrumbDto { Id = articleId, Title = "Entry", Slug = "entry" }]
        });

        rendered.Cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(x => x.PublicSlug, "pub");
            parameters.Add(x => x.ArticlePath, "entry/path");
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("AI Summary:", rendered.Cut.Markup));

        Assert.Contains("Updated", rendered.Cut.Markup);
        Assert.Contains("No content available.", rendered.Cut.Markup);
    }

    [Fact]
    public void PublicWorldPage_Render_ArticleView_WithBody_RendersMarkdownContent()
    {
        var rendered = CreateRenderedSut("pub", "entry/path");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = articleId, Title = "Entry" } });
        rendered.PublicApi.GetPublicArticleAsync("pub", "entry/path").Returns(new ArticleDto
        {
            Id = articleId,
            Title = "Entry",
            Type = ArticleType.WikiArticle,
            Body = "<p>Rendered body</p>",
            Breadcrumbs = [new BreadcrumbDto { Id = articleId, Title = "Entry", Slug = "entry" }]
        });

        rendered.Cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(x => x.PublicSlug, "pub");
            parameters.Add(x => x.ArticlePath, "entry/path");
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("Rendered body", rendered.Cut.Markup));
    }

    [Fact]
    public void PublicWorldPage_Render_ArticleView_WithEmptyBreadcrumbs_DoesNotRenderBreadcrumbs()
    {
        var rendered = CreateRenderedSut("pub", "entry/path");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = articleId, Title = "Entry" } });
        rendered.PublicApi.GetPublicArticleAsync("pub", "entry/path").Returns(new ArticleDto
        {
            Id = articleId,
            Title = "Entry",
            Type = ArticleType.WikiArticle,
            Body = "<p>Rendered body</p>",
            Breadcrumbs = new List<BreadcrumbDto>()
        });

        rendered.Cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(x => x.PublicSlug, "pub");
            parameters.Add(x => x.ArticlePath, "entry/path");
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("Rendered body", rendered.Cut.Markup));
        Assert.DoesNotContain("mb-3 pa-0", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicWorldPage_Render_ArticleView_WithNullBreadcrumbs_DoesNotRenderBreadcrumbs()
    {
        var rendered = CreateRenderedSut("pub", "entry/path");
        var articleId = Guid.NewGuid();
        rendered.PublicApi.GetPublicWorldAsync("pub").Returns(new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        rendered.PublicApi.GetPublicArticleTreeAsync("pub").Returns(new List<ArticleTreeDto> { new() { Id = articleId, Title = "Entry" } });
        rendered.PublicApi.GetPublicArticleAsync("pub", "entry/path").Returns(new ArticleDto
        {
            Id = articleId,
            Title = "Entry",
            Type = ArticleType.WikiArticle,
            Body = "<p>Rendered body</p>",
            Breadcrumbs = null!
        });

        rendered.Cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(x => x.PublicSlug, "pub");
            parameters.Add(x => x.ArticlePath, "entry/path");
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("Rendered body", rendered.Cut.Markup));
        Assert.DoesNotContain("mb-3 pa-0", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicWorldPage_BuildRenderTree_WithWorldNull_ExecutesPageTitleFallbackBranch()
    {
        var sut = new PublicWorldPage();
        sut.PublicSlug = "pub";
        SetPrivateField(sut, "_isLoading", false);
        SetPrivateField(sut, "_world", null);

        InvokeBuildRenderTree(sut);
    }

    [Fact]
    public void PublicWorldPage_BuildRenderTree_WhenArticleLoading_ExecutesSkeletonBranch()
    {
        var sut = new PublicWorldPage
        {
            PublicSlug = "pub",
            ArticlePath = "entry/path"
        };
        SetPrivateField(sut, "_isLoading", false);
        SetPrivateField(sut, "_world", new WorldDetailDto { Name = "Public", OwnerName = "Owner" });
        SetPrivateField(sut, "_currentArticle", null);
        SetPrivateField(sut, "_isLoadingArticle", true);

        InvokeBuildRenderTree(sut);
    }

    [Fact]
    public void PublicWorldPage_Render_WithHeadOutlet_AndMissingWorld_ExecutesPageTitleBranch()
    {
        var publicApi = Substitute.For<IPublicApiService>();
        var markdown = Substitute.For<IMarkdownService>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        markdown.EnsureHtml(Arg.Any<string>()).Returns(ci => ci.Arg<string>());
        publicApi.GetPublicWorldAsync("pub").Returns((WorldDetailDto?)null);

        Services.AddSingleton(publicApi);
        Services.AddSingleton(markdown);
        Services.AddSingleton(jsRuntime);

        RenderComponent<HeadOutlet>();
        var cut = RenderComponent<PublicWorldPage>(parameters =>
        {
            parameters.Add(p => p.PublicSlug, "pub");
        });

        cut.WaitForAssertion(() => Assert.Contains("World Not Found", cut.Markup));
    }

    [Fact]
    public async Task PublicWorldPage_GetBreadcrumbItems_BuildsPathFromPriorRealArticleSlugs()
    {
        var rendered = CreateRenderedSut("pub");
        var worldId = Guid.NewGuid();
        var realA = Guid.NewGuid();
        var realB = Guid.NewGuid();
        var realC = Guid.NewGuid();

        SetPrivateField(rendered.Instance, "_currentArticle", new ArticleDto
        {
            Id = realC,
            Breadcrumbs =
            [
                new BreadcrumbDto { Id = worldId, Title = "World", Slug = "world", IsWorld = true },
                new BreadcrumbDto { Id = Guid.Empty, Title = "Wiki", Slug = "wiki", IsWorld = false },
                new BreadcrumbDto { Id = realA, Title = "A", Slug = "a", IsWorld = false },
                new BreadcrumbDto { Id = realB, Title = "B", Slug = "b", IsWorld = false },
                new BreadcrumbDto { Id = realC, Title = "C", Slug = "c", IsWorld = false }
            ]
        });

        var items = await InvokePrivateWithResultAsync<List<BreadcrumbItem>>(rendered.Instance, "GetBreadcrumbItems");

        Assert.Equal("/w/pub/a/b", items.Single(x => x.Text == "B").Href);
    }

    [Theory]
    [InlineData(ArticleType.WikiArticle, "Wiki Article")]
    [InlineData(ArticleType.Character, "Character")]
    [InlineData(ArticleType.CharacterNote, "Character Note")]
    [InlineData(ArticleType.Session, "Session")]
    [InlineData(ArticleType.SessionNote, "Session Note")]
    [InlineData(ArticleType.Legacy, "Article")]
    [InlineData((ArticleType)999, "Article")]
    public async Task PublicWorldPage_GetArticleTypeLabel_ReturnsExpectedLabel(ArticleType type, string expected)
    {
        var label = await InvokePrivateWithResultAsync<string>(new PublicWorldPage(), "GetArticleTypeLabel", type);

        Assert.Equal(expected, label);
    }

    [Fact]
    public async Task PublicWorldPage_GetPageTitle_WhenWorldNull_UsesFallback()
    {
        var sut = new PublicWorldPage();
        SetPrivateField(sut, "_world", null);

        var title = await InvokePrivateWithResultAsync<string>(sut, "GetPageTitle");

        Assert.Equal("World — Chronicis", title);
    }

    [Fact]
    public async Task PublicWorldPage_GetPageTitle_WhenWorldNamePresent_UsesWorldName()
    {
        var sut = new PublicWorldPage();
        SetPrivateField(sut, "_world", new WorldDetailDto { Name = "My World" });

        var title = await InvokePrivateWithResultAsync<string>(sut, "GetPageTitle");

        Assert.Equal("My World — Chronicis", title);
    }

    [Fact]
    public async Task PublicWorldPage_GetPageTitle_WhenWorldNameWhitespace_UsesFallback()
    {
        var sut = new PublicWorldPage();
        SetPrivateField(sut, "_world", new WorldDetailDto { Name = " " });

        var title = await InvokePrivateWithResultAsync<string>(sut, "GetPageTitle");

        Assert.Equal("World — Chronicis", title);
    }

    private RenderedContext CreateRenderedSut(string publicSlug, string? articlePath = null, Action<IPublicApiService>? configurePublicApi = null)
    {
        var publicApi = Substitute.For<IPublicApiService>();
        var markdown = Substitute.For<IMarkdownService>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        configurePublicApi?.Invoke(publicApi);

        markdown.EnsureHtml(Arg.Any<string>()).Returns(ci => ci.Arg<string>());

        Services.AddSingleton(publicApi);
        Services.AddSingleton(markdown);
        Services.AddSingleton(jsRuntime);

        var cut = RenderComponent<PublicWorldPage>(parameters =>
        {
            parameters.Add(p => p.PublicSlug, publicSlug);
            if (articlePath != null)
            {
                parameters.Add(p => p.ArticlePath, articlePath);
            }
        });

        var navigation = Services.GetRequiredService<NavigationManager>();
        return new RenderedContext(cut, cut.Instance, publicApi, jsRuntime, navigation);
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<PublicWorldPage> cut, string methodName, params object[] args)
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

    private static async Task<T> InvokePrivateWithResultAsync<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.NotNull(method);
        var target = method!.IsStatic ? null : instance;
        var result = method.Invoke(target, args);

        if (result is Task<T> taskOfT)
        {
            return await taskOfT;
        }

        return (T)result!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void InvokeBuildRenderTree(object instance)
    {
        var method = instance.GetType().GetMethod("BuildRenderTree", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var builder = new RenderTreeBuilder();
        method!.Invoke(instance, new object[] { builder });
    }

    private sealed record RenderedContext(
        IRenderedComponent<PublicWorldPage> Cut,
        PublicWorldPage Instance,
        IPublicApiService PublicApi,
        IJSRuntime JsRuntime,
        NavigationManager Navigation);
}
