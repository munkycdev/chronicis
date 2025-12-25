using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Article API operations.
/// All methods return null on failure rather than throwing exceptions.
/// </summary>
public interface IArticleApiService
{
    /// <summary>
    /// Get root-level articles, optionally filtered by world.
    /// </summary>
    Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid? worldId = null);

    /// <summary>
    /// Get all articles (flat list), optionally filtered by world.
    /// </summary>
    Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid? worldId = null);

    /// <summary>
    /// Get child articles of a parent.
    /// </summary>
    Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId);

    /// <summary>
    /// Get full article details by ID.
    /// </summary>
    Task<ArticleDto?> GetArticleDetailAsync(Guid id);

    /// <summary>
    /// Get full article details by ID (alias for GetArticleDetailAsync).
    /// </summary>
    Task<ArticleDto?> GetArticleAsync(Guid id);

    /// <summary>
    /// Get article by its URL path.
    /// </summary>
    Task<ArticleDto?> GetArticleByPathAsync(string path);

    /// <summary>
    /// Create a new article. Returns null on failure.
    /// </summary>
    Task<ArticleDto?> CreateArticleAsync(ArticleCreateDto dto);

    /// <summary>
    /// Update an existing article. Returns null on failure.
    /// </summary>
    Task<ArticleDto?> UpdateArticleAsync(Guid id, ArticleUpdateDto dto);

    /// <summary>
    /// Delete an article. Returns true on success.
    /// </summary>
    Task<bool> DeleteArticleAsync(Guid id);

    /// <summary>
    /// Move an article to a new parent (or to root if newParentId is null).
    /// </summary>
    Task<bool> MoveArticleAsync(Guid articleId, Guid? newParentId);

    /// <summary>
    /// Search articles across title, body, and hashtags.
    /// </summary>
    Task<List<ArticleSearchResultDto>> SearchArticlesAsync(string query);

    /// <summary>
    /// Search articles by title only.
    /// </summary>
    Task<List<ArticleSearchResultDto>> SearchArticlesByTitleAsync(string query);
}
