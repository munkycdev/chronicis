using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services.Articles;

/// <summary>
/// Service for managing external resource links embedded in article content.
/// </summary>
public interface IArticleExternalLinkService
{
    /// <summary>
    /// Synchronizes external links for an article based on its HTML content.
    /// Extracts external link references from HTML and updates the database.
    /// </summary>
    /// <param name="articleId">The article to sync external links for.</param>
    /// <param name="htmlContent">The article's HTML content to parse.</param>
    Task SyncExternalLinksAsync(Guid articleId, string? htmlContent);

    /// <summary>
    /// Gets all external links for a specific article.
    /// </summary>
    /// <param name="articleId">The article ID.</param>
    /// <returns>List of external link DTOs.</returns>
    Task<List<ArticleExternalLinkDto>> GetExternalLinksForArticleAsync(Guid articleId);
}
