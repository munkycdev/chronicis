using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for anonymous public API operations.
/// These endpoints do not require authentication.
/// </summary>
public interface IPublicApiService
{
    /// <summary>
    /// Get a public world by its public slug
    /// </summary>
    Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug);

    /// <summary>
    /// Get the article tree for a public world (only Public visibility articles)
    /// </summary>
    Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug);

    /// <summary>
    /// Get a specific public article by path
    /// </summary>
    Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath);
}
