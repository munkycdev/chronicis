using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

public interface IReadAccessPolicyService
{
    string NormalizePublicSlug(string publicSlug);

    IQueryable<World> ApplyPublicWorldFilter(IQueryable<World> worlds);
    IQueryable<World> ApplyPublicWorldSlugFilter(IQueryable<World> worlds, string publicSlug);
    IQueryable<World> ApplyAuthenticatedWorldFilter(IQueryable<World> worlds, Guid userId);

    IQueryable<Article> ApplyPublicVisibilityFilter(IQueryable<Article> articles);
    IQueryable<Article> ApplyPublicArticleFilter(IQueryable<Article> articles, Guid worldId);
    IQueryable<Article> ApplyTutorialArticleFilter(IQueryable<Article> articles);
    IQueryable<Article> ApplyAuthenticatedWorldArticleFilter(IQueryable<Article> articles, Guid userId);
    IQueryable<Article> ApplyAuthenticatedReadableArticleFilter(IQueryable<Article> articles, Guid userId);

    IQueryable<Campaign> ApplyAuthenticatedCampaignFilter(IQueryable<Campaign> campaigns, Guid userId);
    IQueryable<Arc> ApplyAuthenticatedArcFilter(IQueryable<Arc> arcs, Guid userId);
}
