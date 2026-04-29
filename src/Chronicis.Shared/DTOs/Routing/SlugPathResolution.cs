namespace Chronicis.Shared.Routing;

public sealed record SlugPathResolution(
    ResolvedEntityKind Kind,
    Guid? WorldId,
    Guid? CampaignId,
    Guid? ArcId,
    Guid? SessionId,
    Guid? MapId,
    Guid? ArticleId,
    IReadOnlyList<SlugPathBreadcrumb> Breadcrumbs);

public enum ResolvedEntityKind
{
    World,
    Campaign,
    Arc,
    Session,
    SessionNote,
    MapListing,
    Map,
    WikiArticle
}

public sealed record SlugPathBreadcrumb(
    ResolvedEntityKind Kind,
    string Slug,
    string DisplayName);
