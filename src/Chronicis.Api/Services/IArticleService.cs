using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArticleService
{
    Task<ArticleDto?> GetArticleDetailAsync(Guid id, Guid userId);
    Task<ArticleDto?> GetArticleByPathAsync(string path, Guid userId);
    Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId, Guid userId);
    Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid userId, Guid? worldId = null);
    Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid userId, Guid? worldId = null);
    Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(Guid articleId, Guid? newParentId, Guid userId);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? parentId, Guid userId, Guid? excludeArticleId = null);
    Task<string> GenerateUniqueSlugAsync(string title, Guid? parentId, Guid userId, Guid? excludeArticleId = null);
    Task<string> BuildArticlePathAsync(Guid articleId, Guid userId);
}
