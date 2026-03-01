using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

public interface IArticleDataAccessService
{
    Task AddArticleAsync(Article article);
    Task SaveChangesAsync();
    Task<Article?> FindReadableArticleAsync(Guid articleId, Guid userId);
    Task<bool> IsTutorialSlugUniqueAsync(string slug, Guid? parentId, Guid? excludeArticleId = null);
    Task<string> GenerateTutorialSlugAsync(string title, Guid? parentId, Guid? excludeArticleId = null);
    Task DeleteArticleAndDescendantsAsync(Guid articleId);
    Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId);
    Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId);
    Task<List<ResolvedLinkDto>> ResolveReadableLinksAsync(IEnumerable<Guid> articleIds, Guid userId);
    Task<(bool Found, Guid? WorldId)> TryGetReadableArticleWorldAsync(Guid articleId, Guid userId);
    Task<Article?> GetReadableArticleWithAliasesAsync(Guid articleId, Guid userId);
    Task UpsertAliasesAsync(Article article, IReadOnlyCollection<string> newAliases, Guid userId);
}

