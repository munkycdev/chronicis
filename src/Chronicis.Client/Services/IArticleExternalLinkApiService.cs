using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for interacting with the Article External Links API.
/// </summary>
public interface IArticleExternalLinkApiService
{
    /// <summary>
    /// Gets all external links for a specific article.
    /// </summary>
    /// <param name="articleId">The article ID.</param>
    /// <returns>List of external link DTOs.</returns>
    Task<List<ArticleExternalLinkDto>> GetExternalLinksAsync(Guid articleId);
}
