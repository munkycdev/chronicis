using Chronicis.Shared.DTOs;
using MudBlazor;

namespace Chronicis.Client.Services;

/// <summary>
/// Builds MudBlazor BreadcrumbItems for public world pages from BreadcrumbDto data.
/// Pure data transformation — no API calls or UI state.
/// </summary>
public static class PublicBreadcrumbBuilder
{
    private static readonly HashSet<string> VirtualGroupSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "characters", "wiki", "campaigns", "uncategorized"
    };

    /// <summary>
    /// Converts article breadcrumb DTOs into MudBlazor BreadcrumbItems for public pages.
    /// </summary>
    public static List<BreadcrumbItem> Build(
        string publicSlug,
        ArticleDto article)
    {
        if (article.Breadcrumbs == null || !article.Breadcrumbs.Any())
            return new List<BreadcrumbItem>();

        var items = new List<BreadcrumbItem>();

        foreach (var crumb in article.Breadcrumbs)
        {
            items.Add(ClassifyCrumb(crumb, publicSlug, article));
        }

        return items;
    }

    internal static BreadcrumbItem ClassifyCrumb(
        BreadcrumbDto crumb,
        string publicSlug,
        ArticleDto article)
    {
        if (crumb.IsWorld)
            return new BreadcrumbItem(crumb.Title, $"/w/{publicSlug}");

        if (IsVirtualGroup(crumb))
            return new BreadcrumbItem(crumb.Title, null, disabled: true);

        if (IsVirtualEntity(crumb, article))
            return new BreadcrumbItem(crumb.Title, null, disabled: true);

        if (crumb.Id == article.Id)
            return new BreadcrumbItem(crumb.Title, null, disabled: true);

        var path = BuildPathTo(crumb, article);
        return new BreadcrumbItem(crumb.Title, $"/w/{publicSlug}/{path}");
    }

    internal static bool IsVirtualGroup(BreadcrumbDto crumb)
    {
        return crumb.Id == Guid.Empty || VirtualGroupSlugs.Contains(crumb.Slug);
    }

    internal static bool IsVirtualEntity(BreadcrumbDto crumb, ArticleDto article)
    {
        return (article.CampaignId.HasValue && crumb.Id == article.CampaignId.Value)
            || (article.ArcId.HasValue && crumb.Id == article.ArcId.Value);
    }

    internal static string BuildPathTo(BreadcrumbDto target, ArticleDto article)
    {
        if (article.Breadcrumbs == null)
            return target.Slug;

        var slugs = article.Breadcrumbs
            .Where(b => IsRealArticle(b, article))
            .TakeWhile(b => b.Id != target.Id)
            .Select(b => b.Slug)
            .ToList();

        slugs.Add(target.Slug);
        return string.Join("/", slugs);
    }

    private static bool IsRealArticle(BreadcrumbDto crumb, ArticleDto article)
    {
        return !crumb.IsWorld
            && crumb.Id != Guid.Empty
            && !VirtualGroupSlugs.Contains(crumb.Slug)
            && !(article.CampaignId.HasValue && crumb.Id == article.CampaignId.Value)
            && !(article.ArcId.HasValue && crumb.Id == article.ArcId.Value);
    }
}
