using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Result returned by <see cref="IWikiLinkCommitService.CreateAndLinkAsync"/>.
/// </summary>
public record WikiLinkCreateResult(bool Success, ArticleDto? Article, string? ErrorMessage);

/// <summary>
/// Orchestrates the autocomplete commit decision and article-creation side-effects
/// that are shared across all wiki-link-capable editors.
/// </summary>
public interface IWikiLinkCommitService
{
    /// <summary>
    /// Pure decision function — wraps <see cref="AutocompleteCommitDecision.Decide"/>.
    /// </summary>
    AutocompleteCommitDecision Decide(
        string query,
        int suggestionsCount,
        int selectedIndex,
        bool isExternalQuery,
        bool isMapQuery);

    /// <summary>
    /// Creates a new article via <see cref="IWikiLinkService"/> and returns the result.
    /// JS interop and tree refresh remain the caller's responsibility.
    /// </summary>
    Task<WikiLinkCreateResult> CreateAndLinkAsync(string name, Guid worldId);
}
