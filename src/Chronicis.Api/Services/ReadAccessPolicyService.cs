using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

public sealed class ReadAccessPolicyService : IReadAccessPolicyService
{
    public IQueryable<World> ApplyPublicWorldFilter(IQueryable<World> worlds)
    {
        return worlds.Where(w => w.IsPublic);
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
        // Single predicate instead of Concat of two filtered queries.
        //
        // Why: EF Core translates IQueryable.Concat/Union/Except into SQL set operations (UNION ALL / UNION / EXCEPT),
        // and entities returned from set operations are materialized as UNTRACKED, regardless of the underlying
        // DbSet's tracking behavior. That caused writes (e.g., ArticlesController.UpdateArticle) that read an
        // entity through this filter, mutated it, and called SaveChangesAsync to silently no-op because the change
        // tracker never saw the entity as Modified.
        //
        // A single .Where(...) predicate preserves the same semantic matrix (tutorials + membership-scoped world
        // articles respecting private ownership) while keeping returned entities tracked.
        return articles.Where(a =>
            (a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty)
            ||
            (a.Type != ArticleType.Tutorial
                && a.WorldId != Guid.Empty
                && a.World != null
                && a.World.Members.Any(m => m.UserId == userId)
                && (a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId)));
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

    public bool CanReadWorld(bool isPublic, bool userIsMember) => isPublic || userIsMember;

    public bool CanReadMemberScopedEntity(bool userIsMember) => userIsMember;
}
