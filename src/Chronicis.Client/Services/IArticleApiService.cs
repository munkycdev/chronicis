using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface IArticleApiService
    {
        Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto);
        Task DeleteArticleAsync(Guid id);
        Task<ArticleDto?> GetArticleDetailAsync(Guid id);
        Task<ArticleDto?> GetArticleAsync(Guid id);
        Task<ArticleDto?> GetArticleByPathAsync(string path);
        Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId);
        Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid? worldId = null);
        Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid? worldId = null);
        Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query);
        Task<List<ArticleSearchResultDto>> SearchArticlesByTitleAsync(string query);
        Task<ArticleDto> UpdateArticleAsync(Guid id, ArticleUpdateDto dto);

        /// <summary>
        /// Move an article to a new parent (or to root if newParentId is null).
        /// </summary>
        /// <param name="articleId">The article to move</param>
        /// <param name="newParentId">The new parent ID, or null to make root-level</param>
        /// <returns>True if successful</returns>
        Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId);
    }
}
