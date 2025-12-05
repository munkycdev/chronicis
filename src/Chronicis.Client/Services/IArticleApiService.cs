using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface IArticleApiService
    {
        Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto);
        Task DeleteArticleAsync(int id);
        Task<ArticleDto?> GetArticleDetailAsync(int id);
        Task<ArticleDto?> GetArticleAsync(int id);
        Task<ArticleDto?> GetArticleByPathAsync(string path);
        Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId);
        Task<List<ArticleTreeDto>> GetRootArticlesAsync();
        Task<List<ArticleTreeDto>> GetAllArticlesAsync();
        Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query);
        Task<List<ArticleSearchResultDto>> SearchArticlesByTitleAsync(string query);
        Task<ArticleDto> UpdateArticleAsync(int id, ArticleUpdateDto dto);
        Task<List<BacklinkDto>> GetArticleBacklinksAsync(int articleId);
        Task<List<HashtagDto>> GetArticleHashtagsAsync(int articleId);

        /// <summary>
        /// Move an article to a new parent (or to root if newParentId is null).
        /// </summary>
        /// <param name="articleId">The article to move</param>
        /// <param name="newParentId">The new parent ID, or null to make root-level</param>
        /// <returns>True if successful</returns>
        Task<bool> MoveArticleAsync(int articleId, int? newParentId);
    }
}
