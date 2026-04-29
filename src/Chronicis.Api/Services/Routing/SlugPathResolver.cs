using Chronicis.Shared.Routing;

namespace Chronicis.Api.Services.Routing;

public sealed class SlugPathResolver : ISlugPathResolver
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

    public SlugPathResolver(
        IWorldService worldService,
        IWorldMembershipService membershipService,
        ICampaignService campaignService,
        IArcService arcService,
        ISessionService sessionService,
        IWorldMapService mapService,
        IArticleService articleService,
        IReadAccessPolicyService readAccessPolicy,
        IReservedSlugProvider reservedSlugProvider)
    {
        _worldService = worldService;
        _membershipService = membershipService;
        _campaignService = campaignService;
        _arcService = arcService;
        _sessionService = sessionService;
        _mapService = mapService;
        _articleService = articleService;
        _readAccessPolicy = readAccessPolicy;
        _reservedSlugProvider = reservedSlugProvider;
    }

    public async Task<SlugPathResolution?> ResolveAsync(
        IReadOnlyList<string> segments,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Empty path
        if (segments.Count == 0)
            return null;

        var worldSlug = segments[0];

        // 2. Reserved slug check
        if (_reservedSlugProvider.IsReserved(worldSlug))
            return null;

        // 3. World resolution
        var worldInfo = await _worldService.GetIdBySlugAsync(worldSlug);
        if (worldInfo == null)
            return null;

        var (worldId, isPublic, worldName) = worldInfo.Value;

        var userIsMember = currentUserId.HasValue &&
            await _membershipService.UserHasAccessAsync(worldId, currentUserId.Value);

        if (!_readAccessPolicy.CanReadWorld(isPublic, userIsMember))
            return null;

        var worldBreadcrumb = new SlugPathBreadcrumb(ResolvedEntityKind.World, worldSlug, worldName);

        // 4. One segment → World
        if (segments.Count == 1)
        {
            return new SlugPathResolution(
                Kind: ResolvedEntityKind.World,
                WorldId: worldId,
                CampaignId: null,
                ArcId: null,
                SessionId: null,
                MapId: null,
                ArticleId: null,
                Breadcrumbs: [worldBreadcrumb]);
        }

        var seg1 = segments[1];

        // 5. Two segments — maps short-circuit
        if (seg1 == "maps" && segments.Count == 2)
        {
            return new SlugPathResolution(
                Kind: ResolvedEntityKind.MapListing,
                WorldId: worldId,
                CampaignId: null,
                ArcId: null,
                SessionId: null,
                MapId: null,
                ArticleId: null,
                Breadcrumbs: [worldBreadcrumb]);
        }

        // 6. Three segments — maps/{mapSlug}
        if (seg1 == "maps" && segments.Count == 3)
        {
            var mapSlug = segments[2];
            var mapInfo = await _mapService.GetIdBySlugAsync(worldId, mapSlug);
            if (mapInfo == null)
                return null;

            return new SlugPathResolution(
                Kind: ResolvedEntityKind.Map,
                WorldId: worldId,
                CampaignId: null,
                ArcId: null,
                SessionId: null,
                MapId: mapInfo.Value.Id,
                ArticleId: null,
                Breadcrumbs:
                [
                    worldBreadcrumb,
                    new SlugPathBreadcrumb(ResolvedEntityKind.Map, mapSlug, mapInfo.Value.Name)
                ]);
        }

        // maps with wrong segment count
        if (seg1 == "maps")
            return null;

        // 7. wiki/... short-circuit
        if (seg1 == "wiki")
        {
            if (segments.Count < 3)
                return null;

            var articleSlugs = segments.Skip(2).ToList();
            var articlePath = await _articleService.ResolveWorldArticlePathAsync(
                worldId, articleSlugs, currentUserId, cancellationToken);

            if (articlePath == null)
                return null;

            var articleBreadcrumbs = articlePath.Value.PathBreadcrumbs
                .Select(p => new SlugPathBreadcrumb(ResolvedEntityKind.WikiArticle, p.Slug, p.Title))
                .ToList();

            return new SlugPathResolution(
                Kind: ResolvedEntityKind.WikiArticle,
                WorldId: worldId,
                CampaignId: null,
                ArcId: null,
                SessionId: null,
                MapId: null,
                ArticleId: articlePath.Value.ArticleId,
                Breadcrumbs: [worldBreadcrumb, .. articleBreadcrumbs]);
        }

        // 8. Two segments — campaign
        var campaignSlug = seg1;
        var campaignInfo = await _campaignService.GetIdBySlugAsync(worldId, campaignSlug);
        if (campaignInfo == null)
            return null;

        if (!_readAccessPolicy.CanReadMemberScopedEntity(userIsMember))
            return null;

        var campaignBreadcrumb = new SlugPathBreadcrumb(ResolvedEntityKind.Campaign, campaignSlug, campaignInfo.Value.Name);

        if (segments.Count == 2)
        {
            return new SlugPathResolution(
                Kind: ResolvedEntityKind.Campaign,
                WorldId: worldId,
                CampaignId: campaignInfo.Value.Id,
                ArcId: null,
                SessionId: null,
                MapId: null,
                ArticleId: null,
                Breadcrumbs: [worldBreadcrumb, campaignBreadcrumb]);
        }

        // 9. Three segments — arc
        var arcSlug = segments[2];
        var arcInfo = await _arcService.GetIdBySlugAsync(campaignInfo.Value.Id, arcSlug);
        if (arcInfo == null)
            return null;

        if (!_readAccessPolicy.CanReadMemberScopedEntity(userIsMember))
            return null;

        var arcBreadcrumb = new SlugPathBreadcrumb(ResolvedEntityKind.Arc, arcSlug, arcInfo.Value.Name);

        if (segments.Count == 3)
        {
            return new SlugPathResolution(
                Kind: ResolvedEntityKind.Arc,
                WorldId: worldId,
                CampaignId: campaignInfo.Value.Id,
                ArcId: arcInfo.Value.Id,
                SessionId: null,
                MapId: null,
                ArticleId: null,
                Breadcrumbs: [worldBreadcrumb, campaignBreadcrumb, arcBreadcrumb]);
        }

        // 10. Four segments — session
        var sessionSlug = segments[3];
        var sessionInfo = await _sessionService.GetIdBySlugAsync(arcInfo.Value.Id, sessionSlug);
        if (sessionInfo == null)
            return null;

        if (!_readAccessPolicy.CanReadMemberScopedEntity(userIsMember))
            return null;

        var sessionBreadcrumb = new SlugPathBreadcrumb(ResolvedEntityKind.Session, sessionSlug, sessionInfo.Value.Name);

        if (segments.Count == 4)
        {
            return new SlugPathResolution(
                Kind: ResolvedEntityKind.Session,
                WorldId: worldId,
                CampaignId: campaignInfo.Value.Id,
                ArcId: arcInfo.Value.Id,
                SessionId: sessionInfo.Value.Id,
                MapId: null,
                ArticleId: null,
                Breadcrumbs: [worldBreadcrumb, campaignBreadcrumb, arcBreadcrumb, sessionBreadcrumb]);
        }

        // 11. Five segments — session note
        if (segments.Count == 5)
        {
            var noteSlug = segments[4];
            var noteInfo = await _articleService.GetSessionNoteBySlugAsync(
                sessionInfo.Value.Id, noteSlug, currentUserId, cancellationToken);

            if (noteInfo == null)
                return null;

            if (!_readAccessPolicy.CanReadMemberScopedEntity(userIsMember))
                return null;

            return new SlugPathResolution(
                Kind: ResolvedEntityKind.SessionNote,
                WorldId: worldId,
                CampaignId: campaignInfo.Value.Id,
                ArcId: arcInfo.Value.Id,
                SessionId: sessionInfo.Value.Id,
                MapId: null,
                ArticleId: noteInfo.Value.ArticleId,
                Breadcrumbs:
                [
                    worldBreadcrumb,
                    campaignBreadcrumb,
                    arcBreadcrumb,
                    sessionBreadcrumb,
                    new SlugPathBreadcrumb(ResolvedEntityKind.SessionNote, noteSlug, noteInfo.Value.Title)
                ]);
        }

        // 12. Six or more segments
        return null;
    }
}
