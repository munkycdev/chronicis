using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface IArticleApiService
    {
        Task<ArticleDto> CreateArticleAsync(ArticleCreateDto dto);
        Task DeleteArticleAsync(int id);
        Task<ArticleDto?> GetArticleDetailAsync(int id);
        Task<ArticleDto?> GetArticleAsync(int id);
        Task<List<ArticleTreeDto>> GetChildrenAsync(int parentId);
        Task<List<ArticleTreeDto>> GetRootArticlesAsync();  // Changed to ArticleTreeDto
        Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query);
        Task<ArticleDto> UpdateArticleAsync(int id, ArticleUpdateDto dto);
    }
}