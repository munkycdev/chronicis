using Chronicis.Shared.DTOs;
using MudBlazor;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for building breadcrumb navigation consistently across the application.
/// Single source of truth for hierarchy: Dashboard → World → [Article hierarchy...]
/// </summary>
public interface IBreadcrumbService
{
    /// <summary>
    /// Build breadcrumbs for a World detail page.
    /// Result: Dashboard → World (current, disabled)
    /// </summary>
    List<BreadcrumbItem> ForWorld(WorldDto world, bool currentDisabled = true);

    /// <summary>
    /// Build breadcrumbs for a Campaign detail page.
    /// Result: Dashboard → World → Campaign (current, disabled)
    /// </summary>
    List<BreadcrumbItem> ForCampaign(CampaignDto campaign, WorldDto world, bool currentDisabled = true);

    /// <summary>
    /// Build breadcrumbs for an Arc detail page.
    /// Result: Dashboard → World → Campaign → Arc (current, disabled)
    /// </summary>
    List<BreadcrumbItem> ForArc(ArcDto arc, CampaignDto campaign, WorldDto world, bool currentDisabled = true);

    /// <summary>
    /// Build breadcrumbs for an Article from API breadcrumb data.
    /// Result: Dashboard → World → [Parent Articles...] → Article (current, disabled)
    /// The API breadcrumbs already include the world as the first element.
    /// </summary>
    List<BreadcrumbItem> ForArticle(List<BreadcrumbDto> apiBreadcrumbs);

    /// <summary>
    /// Build the full article URL path from API breadcrumbs.
    /// Returns: /article/world-slug/article-slug/child-slug
    /// </summary>
    string BuildArticleUrl(List<BreadcrumbDto> breadcrumbs);

    /// <summary>
    /// Build the full article URL path for a specific article within the breadcrumb trail.
    /// Returns: /article/world-slug/...up-to-specified-index
    /// </summary>
    string BuildArticleUrlToIndex(List<BreadcrumbDto> breadcrumbs, int index);
}

/// <summary>
/// Implementation of breadcrumb building service.
/// </summary>
public class BreadcrumbService : IBreadcrumbService
{
    /// <summary>
    /// Build breadcrumbs for a World detail page.
    /// </summary>
    public List<BreadcrumbItem> ForWorld(WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: currentDisabled ? null : $"/world/{world.Id}", disabled: currentDisabled)
        };
    }

    /// <summary>
    /// Build breadcrumbs for a Campaign detail page.
    /// </summary>
    public List<BreadcrumbItem> ForCampaign(CampaignDto campaign, WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: $"/world/{world.Id}"),
            new(campaign.Name, href: currentDisabled ? null : $"/campaign/{campaign.Id}", disabled: currentDisabled)
        };
    }

    /// <summary>
    /// Build breadcrumbs for an Arc detail page.
    /// </summary>
    public List<BreadcrumbItem> ForArc(ArcDto arc, CampaignDto campaign, WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: $"/world/{world.Id}"),
            new(campaign.Name, href: $"/campaign/{campaign.Id}"),
            new(arc.Name, href: currentDisabled ? null : $"/arc/{arc.Id}", disabled: currentDisabled)
        };
    }

    /// <summary>
    /// Build breadcrumbs for an Article from API breadcrumb data.
    /// The API breadcrumbs include the world as the first element (IsWorld=true).
    /// </summary>
    public List<BreadcrumbItem> ForArticle(List<BreadcrumbDto> apiBreadcrumbs)
    {
        var result = new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard")
        };

        if (apiBreadcrumbs == null || apiBreadcrumbs.Count == 0)
            return result;

        for (int i = 0; i < apiBreadcrumbs.Count; i++)
        {
            var crumb = apiBreadcrumbs[i];
            var isLast = i == apiBreadcrumbs.Count - 1;

            if (crumb.IsWorld)
            {
                // World breadcrumb - link to world detail page
                result.Add(new BreadcrumbItem(
                    crumb.Title,
                    href: isLast ? null : $"/world/{crumb.Id}",
                    disabled: isLast));
            }
            else
            {
                // Article breadcrumb - build path up to this point
                var path = BuildArticleUrlToIndex(apiBreadcrumbs, i);
                result.Add(new BreadcrumbItem(
                    crumb.Title,
                    href: isLast ? null : path,
                    disabled: isLast));
            }
        }

        return result;
    }

    /// <summary>
    /// Build the full article URL path from API breadcrumbs.
    /// </summary>
    public string BuildArticleUrl(List<BreadcrumbDto> breadcrumbs)
    {
        if (breadcrumbs == null || breadcrumbs.Count == 0)
            return "/dashboard";

        var slugs = breadcrumbs.Select(b => b.Slug);
        return $"/article/{string.Join("/", slugs)}";
    }

    /// <summary>
    /// Build the article URL path up to a specific index in the breadcrumb trail.
    /// </summary>
    public string BuildArticleUrlToIndex(List<BreadcrumbDto> breadcrumbs, int index)
    {
        if (breadcrumbs == null || breadcrumbs.Count == 0 || index < 0)
            return "/dashboard";

        // Clamp index to valid range
        index = Math.Min(index, breadcrumbs.Count - 1);

        // Take slugs up to and including the specified index
        var slugs = breadcrumbs.Take(index + 1).Select(b => b.Slug);
        return $"/article/{string.Join("/", slugs)}";
    }
}
