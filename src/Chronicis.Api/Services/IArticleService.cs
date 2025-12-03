using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArticleService
{
    Task<ArticleDto?> GetArticleDetailAsync(int id, int userId);
    Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId, int userId);
    Task<List<ArticleTreeDto>> GetRootArticlesAsync(int userId);
    Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(int articleId, int? newParentId, int userId);
}
