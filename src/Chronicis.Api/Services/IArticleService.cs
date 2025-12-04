using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArticleService
{
    Task<ArticleDto?> GetArticleDetailAsync(int id, int userId);
    Task<ArticleDto?> GetArticleByPathAsync(string path, int userId);
    Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId, int userId);
    Task<List<ArticleTreeDto>> GetRootArticlesAsync(int userId);
    Task<List<ArticleTreeDto>> GetAllArticlesAsync(int userId);
    Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(int articleId, int? newParentId, int userId);
    Task<bool> IsSlugUniqueAsync(string slug, int? parentId, int userId, int? excludeArticleId = null);
    Task<string> GenerateUniqueSlugAsync(string title, int? parentId, int userId, int? excludeArticleId = null);
    Task<string> BuildArticlePathAsync(int articleId, int userId);
}
