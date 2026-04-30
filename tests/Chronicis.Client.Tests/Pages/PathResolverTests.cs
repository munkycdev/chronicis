using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Layout;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Pages;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Pages;

public class PathResolverTests : TestContext
{
    private readonly IPathApiService _pathApi;
    private readonly IClientReservedSlugProvider _reservedSlugs;
    private readonly TestAuthorizationContext _authContext;

    // Retrieved lazily so service registration is never blocked.
    private FakeNavigationManager Nav => Services.GetRequiredService<FakeNavigationManager>();

    public PathResolverTests()
    {
        _pathApi = Substitute.For<IPathApiService>();
        _reservedSlugs = Substitute.For<IClientReservedSlugProvider>();

        Services.AddSingleton(_pathApi);
        Services.AddSingleton(_reservedSlugs);

        // Register auth infrastructure before any services are resolved.
        _authContext = this.AddTestAuthorization();
        _authContext.SetNotAuthorized();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // Replace LayoutView with a transparent passthrough so PathResolver content renders
        ComponentFactories.Add(
            type => type == typeof(LayoutView),
            _ => new LayoutViewPassthrough());

        // Stub all detail-page components to avoid their transitive dependencies
        ComponentFactories.AddStub<WorldDetail>();
        ComponentFactories.AddStub<CampaignDetail>();
        ComponentFactories.AddStub<ArcDetail>();
        ComponentFactories.AddStub<SessionDetail>();
        ComponentFactories.AddStub<ArticleDetail>();
        ComponentFactories.AddStub<MapListing>();
        ComponentFactories.AddStub<MapDetail>();

        // Stub layout-requiring components rendered by PathResolver
        ComponentFactories.AddStub<LoadingSkeleton>();
        ComponentFactories.AddStub<NotFoundAlert>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // SelectLayout (pure logic, no rendering needed)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SelectLayout_WhenAuthenticated_ReturnsAuthenticatedLayout()
    {
        Assert.Equal(typeof(AuthenticatedLayout), PathResolver.SelectLayout(true));
    }

    [Fact]
    public void SelectLayout_WhenAnonymous_ReturnsPublicLayout()
    {
        Assert.Equal(typeof(PublicLayout), PathResolver.SelectLayout(false));
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetDetailComponentType (pure logic, no rendering needed)
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ResolvedEntityKind.World, typeof(WorldDetail))]
    [InlineData(ResolvedEntityKind.Campaign, typeof(CampaignDetail))]
    [InlineData(ResolvedEntityKind.Arc, typeof(ArcDetail))]
    [InlineData(ResolvedEntityKind.Session, typeof(SessionDetail))]
    [InlineData(ResolvedEntityKind.SessionNote, typeof(ArticleDetail))]
    [InlineData(ResolvedEntityKind.WikiArticle, typeof(ArticleDetail))]
    [InlineData(ResolvedEntityKind.MapListing, typeof(MapListing))]
    [InlineData(ResolvedEntityKind.Map, typeof(MapDetail))]
    public void GetDetailComponentType_ReturnsCorrectType(ResolvedEntityKind kind, Type expected)
    {
        Assert.Equal(expected, PathResolver.GetDetailComponentType(kind));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Empty / null path → no API call, nothing rendered
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PathResolver_NullPath_DoesNotCallApi()
    {
        SetupAnonymousAuth();

        RenderComponent<PathResolver>(p => p.Add(r => r.Path, (string?)null));

        await _pathApi.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PathResolver_EmptyPath_DoesNotCallApi()
    {
        SetupAnonymousAuth();

        RenderComponent<PathResolver>(p => p.Add(r => r.Path, string.Empty));

        await _pathApi.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Reserved root → navigate to /dashboard, no API call
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PathResolver_ReservedRoot_NavigatesToDashboard()
    {
        SetupAnonymousAuth();
        _reservedSlugs.IsReserved("dashboard").Returns(true);

        RenderComponent<PathResolver>(p => p.Add(r => r.Path, "dashboard"));

        Assert.EndsWith("/dashboard", Nav.Uri, StringComparison.OrdinalIgnoreCase);
        await _pathApi.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PathResolver_ReservedRootWithSubPath_NavigatesToDashboard()
    {
        SetupAnonymousAuth();
        _reservedSlugs.IsReserved("settings").Returns(true);

        RenderComponent<PathResolver>(p => p.Add(r => r.Path, "settings/profile"));

        Assert.EndsWith("/dashboard", Nav.Uri, StringComparison.OrdinalIgnoreCase);
        await _pathApi.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Loading state: component shows LoadingSkeleton while API is in flight
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void PathResolver_WhileResolving_ShowsLoadingSkeleton()
    {
        var tcs = new TaskCompletionSource<SlugPathResolution?>();
        _pathApi.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(tcs.Task);
        SetupAnonymousAuth();
        _reservedSlugs.IsReserved(Arg.Any<string>()).Returns(false);

        var cut = RenderComponent<PathResolver>(p => p.Add(r => r.Path, "my-world"));

        cut.FindComponent<Stub<LoadingSkeleton>>();

        tcs.SetResult(null);
        cut.WaitForAssertion(() => cut.FindComponent<Stub<NotFoundAlert>>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Null resolution → NotFoundAlert
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void PathResolver_NullResolution_ShowsNotFoundAlert()
    {
        SetupAnonymousAuth();
        _reservedSlugs.IsReserved(Arg.Any<string>()).Returns(false);
        _pathApi.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((SlugPathResolution?)null);

        var cut = RenderComponent<PathResolver>(p => p.Add(r => r.Path, "unknown-world"));

        cut.FindComponent<Stub<NotFoundAlert>>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Each ResolvedEntityKind → correct child component stub rendered
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void PathResolver_WorldResolution_RendersWorldDetail()
    {
        var worldId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.World, worldId, null, null, null, null, null, []));

        var cut = RenderWithPath("my-world");

        cut.FindComponent<Stub<WorldDetail>>();
    }

    [Fact]
    public void PathResolver_CampaignResolution_RendersCampaignDetail()
    {
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.Campaign, worldId, campaignId, null, null, null, null, []));

        var cut = RenderWithPath("my-world/my-campaign");

        cut.FindComponent<Stub<CampaignDetail>>();
    }

    [Fact]
    public void PathResolver_ArcResolution_RendersArcDetail()
    {
        var arcId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.Arc, Guid.NewGuid(), Guid.NewGuid(), arcId, null, null, null, []));

        var cut = RenderWithPath("my-world/my-campaign/my-arc");

        cut.FindComponent<Stub<ArcDetail>>();
    }

    [Fact]
    public void PathResolver_SessionResolution_RendersSessionDetail()
    {
        var sessionId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.Session, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sessionId, null, null, []));

        var cut = RenderWithPath("my-world/my-campaign/my-arc/session-1");

        cut.FindComponent<Stub<SessionDetail>>();
    }

    [Fact]
    public void PathResolver_SessionNoteResolution_RendersArticleDetail()
    {
        var articleId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.SessionNote, Guid.NewGuid(), null, null, null, null, articleId, []));

        var cut = RenderWithPath("my-world/session-1/my-note");

        cut.FindComponent<Stub<ArticleDetail>>();
    }

    [Fact]
    public void PathResolver_WikiArticleResolution_RendersArticleDetail()
    {
        var articleId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.WikiArticle, Guid.NewGuid(), null, null, null, null, articleId, []));

        var cut = RenderWithPath("my-world/my-article");

        cut.FindComponent<Stub<ArticleDetail>>();
    }

    [Fact]
    public void PathResolver_WikiArticleResolution_PassesArticleIdToArticleDetail()
    {
        var articleId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.WikiArticle, Guid.NewGuid(), null, null, null, null, articleId, []));

        var cut = RenderWithPath("my-world/my-article");

        var stub = cut.FindComponent<Stub<ArticleDetail>>();
        Assert.Equal(articleId, stub.Instance.Parameters.Get(c => c.ArticleId));
    }

    [Fact]
    public void PathResolver_SessionNoteResolution_PassesArticleIdToArticleDetail()
    {
        var articleId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.SessionNote, Guid.NewGuid(), null, null, null, null, articleId, []));

        var cut = RenderWithPath("my-world/session-1/my-note");

        var stub = cut.FindComponent<Stub<ArticleDetail>>();
        Assert.Equal(articleId, stub.Instance.Parameters.Get(c => c.ArticleId));
    }

    [Fact]
    public void PathResolver_MapListingResolution_RendersMapListing()
    {
        var worldId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.MapListing, worldId, null, null, null, null, null, []));

        var cut = RenderWithPath("my-world/maps");

        cut.FindComponent<Stub<MapListing>>();
    }

    [Fact]
    public void PathResolver_MapResolution_RendersMapDetail()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.Map, worldId, null, null, null, mapId, null, []));

        var cut = RenderWithPath("my-world/maps/my-map");

        cut.FindComponent<Stub<MapDetail>>();
    }

    [Fact]
    public void PathResolver_TutorialResolution_RendersArticleDetail()
    {
        var articleId = Guid.NewGuid();
        SetupResolution(new SlugPathResolution(ResolvedEntityKind.Tutorial, null, null, null, null, null, articleId, []));

        var cut = RenderWithPath("tutorials/my-tutorial");

        cut.FindComponent<Stub<ArticleDetail>>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Unknown ResolvedEntityKind → null from GetDetailComponentType, nothing rendered
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetDetailComponentType_UnknownKind_ReturnsNull()
    {
        var unknownKind = (ResolvedEntityKind)999;

        Assert.Null(PathResolver.GetDetailComponentType(unknownKind));
    }

    [Fact]
    public void PathResolver_UnknownKind_RendersNothing()
    {
        var unknownKind = (ResolvedEntityKind)999;
        SetupResolution(new SlugPathResolution(unknownKind, null, null, null, null, null, null, []));

        var cut = RenderWithPath("some-path");

        // No detail component stub, no loading, no not-found — renders empty content
        Assert.Empty(cut.FindComponents<Stub<LoadingSkeleton>>());
        Assert.Empty(cut.FindComponents<Stub<NotFoundAlert>>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private void SetupAnonymousAuth() => _authContext.SetNotAuthorized();

    private void SetupAuthenticatedAuth() => _authContext.SetAuthorized("test");

    private void SetupResolution(SlugPathResolution resolution)
    {
        SetupAnonymousAuth();
        _reservedSlugs.IsReserved(Arg.Any<string>()).Returns(false);
        _pathApi.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(resolution);
    }

    private IRenderedComponent<PathResolver> RenderWithPath(string path) =>
        RenderComponent<PathResolver>(p => p.Add(r => r.Path, path));

    /// <summary>
    /// Replaces LayoutView with a pass-through that renders ChildContent directly,
    /// removing the need to register layout-component dependencies in unit tests.
    /// </summary>
    private sealed class LayoutViewPassthrough : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public Type? Layout { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            ChildContent?.Invoke(builder);
    }
}
