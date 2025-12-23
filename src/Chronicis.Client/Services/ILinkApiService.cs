using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for interacting with wiki link APIs.
/// </summary>
public interface ILinkApiService
{
    /// <summary>
    /// Gets link suggestions for autocomplete based on a search query.
    /// </summary>
    /// <param name="worldId">The world to search within.</param>
    /// <param name="query">The search query (must be 3+ characters).</param>
    /// <returns>List of matching article suggestions.</returns>
    Task<List<LinkSuggestionDto>> GetSuggestionsAsync(Guid worldId, string query);

    /// <summary>
    /// Gets all articles that link to the specified article (backlinks).
    /// </summary>
    /// <param name="articleId">The article to find backlinks for.</param>
    /// <returns>List of articles linking to this article.</returns>
    Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId);

    /// <summary>
    /// Gets all articles that this article links to (outgoing links).
    /// </summary>
    /// <param name="articleId">The article to find outgoing links for.</param>
    /// <returns>List of articles this article links to.</returns>
    Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId);

    /// <summary>
    /// Resolves multiple article IDs to check if they exist (for broken link detection).
    /// </summary>
    /// <param name="articleIds">The article IDs to resolve.</param>
    /// <returns>Dictionary mapping each ID to its resolution info.</returns>
    Task<Dictionary<Guid, ResolvedLinkDto>> ResolveLinksAsync(List<Guid> articleIds);

    /// <summary>
    /// Scans article content and returns modified content with wiki links auto-inserted.
    /// </summary>
    /// <param name="articleId">The article to auto-link.</param>
    /// <param name="body">The current body content.</param>
    /// <returns>Response with modified body and match details.</returns>
    Task<AutoLinkResponseDto?> AutoLinkAsync(Guid articleId, string body);
}
