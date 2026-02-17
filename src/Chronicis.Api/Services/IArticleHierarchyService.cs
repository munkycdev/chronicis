using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Centralised service for walking article parent hierarchies.
/// Replaces duplicated parent-walk logic formerly in ArticleService,
/// PublicWorldService, SearchController, ArticlesController, and WorldsController.
/// </summary>
public interface IArticleHierarchyService
{
    /// <summary>
    /// Build an ordered breadcrumb trail from the root (optionally including the world)
    /// down to the specified article.
    /// </summary>
    Task<List<BreadcrumbDto>> BuildBreadcrumbsAsync(Guid articleId, HierarchyWalkOptions? options = null);

    /// <summary>
    /// Build a slash-separated path string from slugs (e.g. "world-slug/parent-slug/child-slug").
    /// Convenience wrapper around <see cref="BuildBreadcrumbsAsync"/>.
    /// </summary>
    Task<string> BuildPathAsync(Guid articleId, HierarchyWalkOptions? options = null);

    /// <summary>
    /// Build a human-readable display path of titles joined with " / ".
    /// Optionally strips the first level (world root) for display in link suggestions / backlinks.
    /// </summary>
    Task<string> BuildDisplayPathAsync(Guid articleId, bool stripFirstLevel = true);
}

/// <summary>
/// Options that control how the hierarchy walk behaves.
/// </summary>
public class HierarchyWalkOptions
{
    /// <summary>
    /// When true, only articles with Public visibility are included in the walk.
    /// If a non-public article is encountered mid-chain the walk stops.
    /// </summary>
    public bool PublicOnly { get; set; } = false;

    /// <summary>
    /// When true, a World breadcrumb is prepended to the result.
    /// Requires the target article to have a WorldId.
    /// </summary>
    public bool IncludeWorldBreadcrumb { get; set; } = true;

    /// <summary>
    /// When true, virtual group breadcrumbs (Campaigns, Player Characters, Wiki)
    /// are inserted between the World breadcrumb and the article chain.
    /// Only meaningful when <see cref="IncludeWorldBreadcrumb"/> is also true.
    /// </summary>
    public bool IncludeVirtualGroups { get; set; } = false;

    /// <summary>
    /// When true (default), the target article itself is included as the last breadcrumb.
    /// Set to false when you only need the ancestor chain above the article.
    /// </summary>
    public bool IncludeCurrentArticle { get; set; } = true;

    /// <summary>
    /// Pre-resolved world metadata. When supplied, skips the world lookup query.
    /// Required when <see cref="IncludeVirtualGroups"/> is true.
    /// </summary>
    public WorldContext? World { get; set; }
}

/// <summary>
/// Pre-resolved world information to avoid redundant DB lookups
/// when the caller already has it.
/// </summary>
public class WorldContext
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
