using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Routing;
using Chronicis.Shared.Routing;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services.Routing;

[ExcludeFromCodeCoverage]
public class SlugPathResolverTests
{
    private readonly IWorldService _worldService;
    private readonly IWorldMembershipService _membershipService;
    private readonly ICampaignService _campaignService;
    private readonly IArcService _arcService;
    private readonly ISessionService _sessionService;
    private readonly IWorldMapService _mapService;
    private readonly IArticleService _articleService;
    private readonly IReadAccessPolicyService _readAccessPolicy;
    private readonly IReservedSlugProvider _reservedSlugProvider;
    private readonly SlugPathResolver _sut;

    private static readonly Guid WorldId = Guid.Parse("11000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("22000000-0000-0000-0000-000000000001");
    private static readonly Guid CampaignId = Guid.Parse("33000000-0000-0000-0000-000000000001");
    private static readonly Guid ArcId = Guid.Parse("44000000-0000-0000-0000-000000000001");
    private static readonly Guid SessionId = Guid.Parse("55000000-0000-0000-0000-000000000001");
    private static readonly Guid MapId = Guid.Parse("66000000-0000-0000-0000-000000000001");
    private static readonly Guid ArticleId = Guid.Parse("77000000-0000-0000-0000-000000000001");

    public SlugPathResolverTests()
    {
        _worldService = Substitute.For<IWorldService>();
        _membershipService = Substitute.For<IWorldMembershipService>();
        _campaignService = Substitute.For<ICampaignService>();
        _arcService = Substitute.For<IArcService>();
        _sessionService = Substitute.For<ISessionService>();
        _mapService = Substitute.For<IWorldMapService>();
        _articleService = Substitute.For<IArticleService>();
        _readAccessPolicy = Substitute.For<IReadAccessPolicyService>();
        _reservedSlugProvider = Substitute.For<IReservedSlugProvider>();

        _sut = new SlugPathResolver(
            _worldService, _membershipService, _campaignService, _arcService,
            _sessionService, _mapService, _articleService, _readAccessPolicy, _reservedSlugProvider);

        // Default: world "test-world" exists, is public, user is a member
        _reservedSlugProvider.IsReserved(Arg.Any<string>()).Returns(false);
        _worldService.GetIdBySlugAsync("test-world").Returns(
            Task.FromResult<(Guid Id, bool IsPublic, string Name)?>(
                (WorldId, true, "Test World")));
        _membershipService.UserHasAccessAsync(WorldId, UserId).Returns(true);
        _readAccessPolicy.CanReadWorld(Arg.Any<bool>(), Arg.Any<bool>()).Returns(true);
        _readAccessPolicy.CanReadMemberScopedEntity(Arg.Any<bool>()).Returns(true);
        _campaignService.GetIdBySlugAsync(WorldId, "my-campaign").Returns(
            Task.FromResult<(Guid Id, string Name)?>(
                (CampaignId, "My Campaign")));
        _arcService.GetIdBySlugAsync(CampaignId, "arc-1").Returns(
            Task.FromResult<(Guid Id, string Name)?>(
                (ArcId, "Arc 1")));
        _sessionService.GetIdBySlugAsync(ArcId, "session-1").Returns(
            Task.FromResult<(Guid Id, string Name)?>(
                (SessionId, "Session 1")));
        _mapService.GetIdBySlugAsync(WorldId, "main-map").Returns(
            Task.FromResult<(Guid Id, string Name)?>(
                (MapId, "Main Map")));
        _articleService.ResolveWorldArticlePathAsync(
                WorldId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(Guid ArticleId, IReadOnlyList<(string Slug, string Title)> PathBreadcrumbs)?>(
                (ArticleId, new List<(string, string)> { ("wiki-article", "Wiki Article") })));
        _articleService.GetSessionNoteBySlugAsync(
                SessionId, "note-1", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(Guid ArticleId, string Title)?>(
                (ArticleId, "My Session Note")));
    }

    // ── Empty / reserved ────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_EmptySegments_ReturnsNull()
    {
        var result = await _sut.ResolveAsync([], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ReservedSlug_ReturnsNull()
    {
        _reservedSlugProvider.IsReserved("admin").Returns(true);
        var result = await _sut.ResolveAsync(["admin"], UserId);
        Assert.Null(result);
    }

    // ── World lookup ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_UnknownWorld_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["unknown-world"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WorldNotAccessible_ReturnsNull()
    {
        _readAccessPolicy.CanReadWorld(Arg.Any<bool>(), Arg.Any<bool>()).Returns(false);
        var result = await _sut.ResolveAsync(["test-world"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_OneSegment_ReturnsWorld()
    {
        var result = await _sut.ResolveAsync(["test-world"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.World, result!.Kind);
        Assert.Equal(WorldId, result.WorldId);
        Assert.Null(result.CampaignId);
        Assert.Single(result.Breadcrumbs);
        Assert.Equal("Test World", result.Breadcrumbs[0].DisplayName);
    }

    // ── Maps ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_TwoSegments_Maps_ReturnsMapListing()
    {
        var result = await _sut.ResolveAsync(["test-world", "maps"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.MapListing, result!.Kind);
        Assert.Equal(WorldId, result.WorldId);
        Assert.Single(result.Breadcrumbs);
    }

    [Fact]
    public async Task ResolveAsync_ThreeSegments_MapFound_ReturnsMap()
    {
        var result = await _sut.ResolveAsync(["test-world", "maps", "main-map"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.Map, result!.Kind);
        Assert.Equal(MapId, result.MapId);
        Assert.Equal(2, result.Breadcrumbs.Count);
        Assert.Equal("Main Map", result.Breadcrumbs[1].DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_ThreeSegments_MapNotFound_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "maps", "no-such-map"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_FourSegments_MapsPrefix_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "maps", "main-map", "extra"], UserId);
        Assert.Null(result);
    }

    // ── Wiki ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_WikiWithTwoSegments_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "wiki"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_WikiArticleFound_ReturnsWikiArticle()
    {
        var result = await _sut.ResolveAsync(["test-world", "wiki", "wiki-article"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.WikiArticle, result!.Kind);
        Assert.Equal(ArticleId, result.ArticleId);
        Assert.Equal(2, result.Breadcrumbs.Count);
    }

    [Fact]
    public async Task ResolveAsync_WikiArticleNotFound_ReturnsNull()
    {
        _articleService.ResolveWorldArticlePathAsync(
                WorldId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(Guid ArticleId, IReadOnlyList<(string Slug, string Title)> PathBreadcrumbs)?>(null));

        var result = await _sut.ResolveAsync(["test-world", "wiki", "no-article"], UserId);
        Assert.Null(result);
    }

    // ── Campaign ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_TwoSegments_CampaignFound_ReturnsCampaign()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.Campaign, result!.Kind);
        Assert.Equal(CampaignId, result.CampaignId);
        Assert.Equal(2, result.Breadcrumbs.Count);
        Assert.Equal("My Campaign", result.Breadcrumbs[1].DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_TwoSegments_CampaignNotFound_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "no-campaign"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_CampaignFound_PolicyDenies_ReturnsNull()
    {
        _readAccessPolicy.CanReadMemberScopedEntity(Arg.Any<bool>()).Returns(false);
        var result = await _sut.ResolveAsync(["test-world", "my-campaign"], UserId);
        Assert.Null(result);
    }

    // ── Arc ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_ThreeSegments_ArcFound_ReturnsArc()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.Arc, result!.Kind);
        Assert.Equal(ArcId, result.ArcId);
        Assert.Equal(3, result.Breadcrumbs.Count);
    }

    [Fact]
    public async Task ResolveAsync_ThreeSegments_ArcNotFound_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "no-arc"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ArcFound_PolicyDenies_ReturnsNull()
    {
        _readAccessPolicy.CanReadMemberScopedEntity(Arg.Any<bool>())
            .Returns(true, false); // campaign passes, arc fails
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1"], UserId);
        Assert.Null(result);
    }

    // ── Session ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_FourSegments_SessionFound_ReturnsSession()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "session-1"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.Session, result!.Kind);
        Assert.Equal(SessionId, result.SessionId);
        Assert.Equal(4, result.Breadcrumbs.Count);
    }

    [Fact]
    public async Task ResolveAsync_FourSegments_SessionNotFound_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "no-session"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_SessionFound_PolicyDenies_ReturnsNull()
    {
        _readAccessPolicy.CanReadMemberScopedEntity(Arg.Any<bool>())
            .Returns(true, true, false); // campaign + arc pass, session fails
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "session-1"], UserId);
        Assert.Null(result);
    }

    // ── Session Note ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_FiveSegments_NoteFound_ReturnsSessionNote()
    {
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "session-1", "note-1"], UserId);

        Assert.NotNull(result);
        Assert.Equal(ResolvedEntityKind.SessionNote, result!.Kind);
        Assert.Equal(ArticleId, result.ArticleId);
        Assert.Equal(5, result.Breadcrumbs.Count);
        Assert.Equal("My Session Note", result.Breadcrumbs[4].DisplayName);
    }

    [Fact]
    public async Task ResolveAsync_FiveSegments_NoteNotFound_ReturnsNull()
    {
        _articleService.GetSessionNoteBySlugAsync(
                SessionId, "no-note", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(Guid ArticleId, string Title)?>(null));

        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "session-1", "no-note"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NoteFound_PolicyDenies_ReturnsNull()
    {
        _readAccessPolicy.CanReadMemberScopedEntity(Arg.Any<bool>())
            .Returns(true, true, true, false); // campaign + arc + session pass, note fails
        var result = await _sut.ResolveAsync(["test-world", "my-campaign", "arc-1", "session-1", "note-1"], UserId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_SixSegments_ReturnsNull()
    {
        var result = await _sut.ResolveAsync(
            ["test-world", "my-campaign", "arc-1", "session-1", "note-1", "extra"], UserId);
        Assert.Null(result);
    }
}
