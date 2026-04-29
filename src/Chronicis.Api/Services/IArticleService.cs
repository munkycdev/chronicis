using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Api.Services;

public interface IArticleService
{
    Task<ArticleDto?> GetArticleDetailAsync(Guid id, Guid userId);
    Task<ArticleDto?> GetArticleByPathAsync(string path, Guid userId);
    Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId, Guid userId);
    Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid userId, Guid? worldId = null);
    Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid userId, Guid? worldId = null);
    Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(Guid articleId, Guid? newParentId, Guid? newSessionId, Guid userId);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null, ArticleType articleType = ArticleType.WikiArticle, Guid? sessionId = null);
    Task<string> GenerateUniqueSlugAsync(string title, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null, ArticleType articleType = ArticleType.WikiArticle, Guid? sessionId = null);
    Task<string> BuildArticlePathAsync(Guid articleId, Guid userId);

    /// <summary>
    /// Resolve a hierarchical article path within a world, applying read policy for the given user.
    /// Returns (articleId, path breadcrumbs slug→title from root to leaf) or null if not found/denied.
    /// </summary>
    Task<(Guid ArticleId, IReadOnlyList<(string Slug, string Title)> PathBreadcrumbs)?> ResolveWorldArticlePathAsync(
        Guid worldId,
        IReadOnlyList<string> slugs,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find a SessionNote article by sessionId + slug, applying read policy for the given user.
    /// Returns (articleId, title) or null if not found/denied.
    /// </summary>
    Task<(Guid ArticleId, string Title)?> GetSessionNoteBySlugAsync(
        Guid sessionId,
        string slug,
        Guid? userId,
        CancellationToken cancellationToken = default);
}
