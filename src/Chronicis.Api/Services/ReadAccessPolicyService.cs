using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

public class ReadAccessPolicyService : IReadAccessPolicyService
{
    public string NormalizePublicSlug(string publicSlug)
    {
        return publicSlug.Trim().ToLowerInvariant();
    }

    public IQueryable<World> ApplyPublicWorldFilter(IQueryable<World> worlds)
    {
        return worlds.Where(w => w.IsPublic);
    }

    public IQueryable<World> ApplyPublicWorldSlugFilter(IQueryable<World> worlds, string publicSlug)
    {
        var normalizedSlug = NormalizePublicSlug(publicSlug);
        return ApplyPublicWorldFilter(worlds)
            .Where(w => w.PublicSlug == normalizedSlug);
    }

    public IQueryable<World> ApplyAuthenticatedWorldFilter(IQueryable<World> worlds, Guid userId)
    {
        return worlds.Where(w => w.Members.Any(m => m.UserId == userId));
    }

    public IQueryable<Article> ApplyPublicVisibilityFilter(IQueryable<Article> articles)
    {
        return articles.Where(a => a.Visibility == ArticleVisibility.Public);
    }

    public IQueryable<Article> ApplyPublicArticleFilter(IQueryable<Article> articles, Guid worldId)
    {
        return ApplyPublicVisibilityFilter(articles)
            .Where(a => a.WorldId == worldId);
    }

    public IQueryable<Article> ApplyTutorialArticleFilter(IQueryable<Article> articles)
    {
        return articles.Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);
    }

    public IQueryable<Article> ApplyAuthenticatedWorldArticleFilter(IQueryable<Article> articles, Guid userId)
    {
        return articles
            .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId))
            .Where(a => a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId);
    }

    public IQueryable<Article> ApplyAuthenticatedReadableArticleFilter(IQueryable<Article> articles, Guid userId)
    {
        var worldScoped = ApplyAuthenticatedWorldArticleFilter(articles, userId);
        var tutorials = ApplyTutorialArticleFilter(articles);
        return worldScoped.Concat(tutorials);
    }

    public IQueryable<Campaign> ApplyAuthenticatedCampaignFilter(IQueryable<Campaign> campaigns, Guid userId)
    {
        return campaigns
            .Where(c => c.World != null && c.World.Members.Any(m => m.UserId == userId));
    }

    public IQueryable<Arc> ApplyAuthenticatedArcFilter(IQueryable<Arc> arcs, Guid userId)
    {
        return arcs
            .Where(a => a.Campaign != null
                        && a.Campaign.World != null
                        && a.Campaign.World.Members.Any(m => m.UserId == userId));
    }
}
