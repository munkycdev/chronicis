using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for anonymous public access to worlds.
/// All methods return only publicly visible content.
/// </summary>
public interface IPublicWorldService
{
    /// <summary>
    /// Get a public world by its public slug.
    /// Returns null if world doesn't exist or is not public.
    /// </summary>
    Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug);

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// </summary>
    Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug);

    /// <summary>
    /// Get a specific article by path in a public world.
    /// Returns null if article doesn't exist, world is not public, or article is not Public visibility.
    /// </summary>
    Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath);

    /// <summary>
    /// Resolve an article ID to its public URL path.
    /// Returns null if the article doesn't exist, is not public, or doesn't belong to the specified world.
    /// </summary>
    Task<string?> GetPublicArticlePathAsync(string publicSlug, Guid articleId);

    /// <summary>
    /// Resolve a public inline-image document ID to a fresh download URL.
    /// Returns null when the document is not attached to a public article in a public world.
    /// </summary>
    Task<string?> GetPublicDocumentDownloadUrlAsync(Guid documentId);
}
