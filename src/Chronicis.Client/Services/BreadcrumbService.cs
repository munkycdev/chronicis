using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using MudBlazor;

namespace Chronicis.Client.Services;

public interface IBreadcrumbService
{
    List<BreadcrumbItem> ForWorld(WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForCampaign(CampaignDto campaign, WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForArc(ArcDto arc, CampaignDto campaign, WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForArticle(List<BreadcrumbDto> apiBreadcrumbs);

    /// <summary>Build the full slug-based URL path from API breadcrumbs.</summary>
    string BuildArticleUrl(List<BreadcrumbDto> breadcrumbs);

    /// <summary>Build the slug-based URL path for a specific article within the breadcrumb trail.</summary>
    string BuildArticleUrlToIndex(List<BreadcrumbDto> breadcrumbs, int index);
}

public class BreadcrumbService : IBreadcrumbService
{
    private readonly IAppUrlBuilder _urlBuilder;

    public BreadcrumbService(IAppUrlBuilder urlBuilder)
    {
        _urlBuilder = urlBuilder;
    }

    public List<BreadcrumbItem> ForWorld(WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: currentDisabled ? null : _urlBuilder.ForWorld(world.Slug), disabled: currentDisabled)
        };
    }

    public List<BreadcrumbItem> ForCampaign(CampaignDto campaign, WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: _urlBuilder.ForWorld(world.Slug)),
            new(campaign.Name, href: currentDisabled ? null : _urlBuilder.ForCampaign(world.Slug, campaign.Slug), disabled: currentDisabled)
        };
    }

    public List<BreadcrumbItem> ForArc(ArcDto arc, CampaignDto campaign, WorldDto world, bool currentDisabled = true)
    {
        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(world.Name, href: _urlBuilder.ForWorld(world.Slug)),
            new(campaign.Name, href: _urlBuilder.ForCampaign(world.Slug, campaign.Slug)),
            new(arc.Name, href: currentDisabled ? null : _urlBuilder.ForArc(world.Slug, campaign.Slug, arc.Slug), disabled: currentDisabled)
        };
    }

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
                result.Add(new BreadcrumbItem(
                    crumb.Title,
                    href: isLast ? null : _urlBuilder.ForWorld(crumb.Slug),
                    disabled: isLast));
            }
            else
            {
                var path = BuildArticleUrlToIndex(apiBreadcrumbs, i);
                result.Add(new BreadcrumbItem(
                    crumb.Title,
                    href: isLast ? null : path,
                    disabled: isLast));
            }
        }

        return result;
    }

    public string BuildArticleUrl(List<BreadcrumbDto> breadcrumbs)
    {
        if (breadcrumbs == null || breadcrumbs.Count == 0)
            return "/dashboard";

        // Breadcrumbs from the server are pre-shaped to the URL hierarchy:
        // wiki articles include a virtual "wiki" segment; session notes include
        // campaign/arc/session segments. Join slugs directly without re-wrapping.
        return "/" + string.Join("/", breadcrumbs.Select(b => b.Slug));
    }

    public string BuildArticleUrlToIndex(List<BreadcrumbDto> breadcrumbs, int index)
    {
        if (breadcrumbs == null || breadcrumbs.Count == 0 || index < 0)
            return "/dashboard";

        index = Math.Min(index, breadcrumbs.Count - 1);
        return "/" + string.Join("/", breadcrumbs.Take(index + 1).Select(b => b.Slug));
    }
}
