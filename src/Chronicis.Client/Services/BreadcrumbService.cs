using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using MudBlazor;

namespace Chronicis.Client.Services;

public interface IBreadcrumbService
{
    List<BreadcrumbItem> ForWorld(WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForCampaign(CampaignDto campaign, WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForArc(ArcDto arc, CampaignDto campaign, WorldDto world, bool currentDisabled = true);
    List<BreadcrumbItem> ForArticle(ArticleDto article);
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

    public List<BreadcrumbItem> ForArticle(ArticleDto article)
    {
        var result = new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard")
        };

        var breadcrumbs = article.Breadcrumbs;
        if (breadcrumbs == null || breadcrumbs.Count == 0)
            return result;

        return article.Type == ArticleType.SessionNote
            ? BuildSessionNoteBreadcrumbs(result, breadcrumbs)
            : BuildWikiArticleBreadcrumbs(result, breadcrumbs);
    }

    private List<BreadcrumbItem> BuildWikiArticleBreadcrumbs(List<BreadcrumbItem> result, List<BreadcrumbDto> breadcrumbs)
    {
        var worldSlug = breadcrumbs.FirstOrDefault(b => b.IsWorld)?.Slug ?? string.Empty;
        var nonWorldSlugs = breadcrumbs.Where(b => !b.IsWorld).Select(b => b.Slug).ToList();

        for (int i = 0; i < breadcrumbs.Count; i++)
        {
            var crumb = breadcrumbs[i];
            var isLast = i == breadcrumbs.Count - 1;

            string? href = null;
            if (!isLast)
            {
                if (crumb.IsWorld)
                    href = _urlBuilder.ForWorld(crumb.Slug);
                else
                {
                    var nonWorldIndex = breadcrumbs.Take(i + 1).Count(b => !b.IsWorld);
                    href = _urlBuilder.ForWikiArticle(worldSlug, nonWorldSlugs.Take(nonWorldIndex).ToList());
                }
            }

            result.Add(new BreadcrumbItem(crumb.Title, href: href, disabled: isLast));
        }
        return result;
    }

    private List<BreadcrumbItem> BuildSessionNoteBreadcrumbs(List<BreadcrumbItem> result, List<BreadcrumbDto> breadcrumbs)
    {
        // [0]=World, [1]=Campaign, [2]=Arc, [3]=Session, [4]=Note
        for (int i = 0; i < breadcrumbs.Count; i++)
        {
            var crumb = breadcrumbs[i];
            var isLast = i == breadcrumbs.Count - 1;

            string? href = isLast ? null : i switch
            {
                0 => _urlBuilder.ForWorld(crumb.Slug),
                1 => _urlBuilder.ForCampaign(breadcrumbs[0].Slug, crumb.Slug),
                2 => _urlBuilder.ForArc(breadcrumbs[0].Slug, breadcrumbs[1].Slug, crumb.Slug),
                3 => _urlBuilder.ForSession(breadcrumbs[0].Slug, breadcrumbs[1].Slug, breadcrumbs[2].Slug, crumb.Slug),
                _ => null
            };

            result.Add(new BreadcrumbItem(crumb.Title, href: href, disabled: isLast));
        }
        return result;
    }
}
