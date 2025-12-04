using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Utilities;

/// <summary>
/// Helper class for building hierarchical article paths from breadcrumbs.
/// </summary>
public static class ArticlePathBuilder
{
    /// <summary>
    /// Builds the full URL path for an article from its breadcrumbs.
    /// Example: ["sword-coast", "waterdeep", "castle-ward"] -> "sword-coast/waterdeep/castle-ward"
    /// </summary>
    public static string BuildPath(List<BreadcrumbDto> breadcrumbs)
    {
        if (breadcrumbs == null || breadcrumbs.Count == 0)
            return string.Empty;

        return string.Join("/", breadcrumbs.Select(b => b.Slug));
    }

    /// <summary>
    /// Builds the full URL for an article.
    /// Example: /article/sword-coast/waterdeep/castle-ward
    /// </summary>
    public static string BuildArticleUrl(List<BreadcrumbDto> breadcrumbs)
    {
        var path = BuildPath(breadcrumbs);
        return string.IsNullOrEmpty(path) ? "/article" : $"/article/{path}";
    }

    /// <summary>
    /// Builds URL for an article at a specific breadcrumb index (for clickable breadcrumbs).
    /// </summary>
    public static string BuildArticleUrlAtIndex(List<BreadcrumbDto> breadcrumbs, int index)
    {
        if (breadcrumbs == null || index < 0 || index >= breadcrumbs.Count)
            return "/";

        var pathCrumbs = breadcrumbs.Take(index + 1).ToList();
        return BuildArticleUrl(pathCrumbs);
    }
}
