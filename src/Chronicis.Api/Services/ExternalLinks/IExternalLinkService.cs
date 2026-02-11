namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Consolidated service for external link operations including
/// suggestion search, content retrieval, and validation.
/// </summary>
public interface IExternalLinkService
{
    /// <summary>
    /// Searches for external link suggestions from a specific provider.
    /// Respects world-level provider enablement when worldId is provided.
    /// </summary>
    Task<IReadOnlyList<ExternalLinkSuggestion>> GetSuggestionsAsync(
        Guid? worldId,
        string source,
        string query,
        CancellationToken ct);

    /// <summary>
    /// Retrieves the full content for an external link from its provider.
    /// </summary>
    Task<ExternalLinkContent?> GetContentAsync(
        string source,
        string id,
        CancellationToken ct);

    /// <summary>
    /// Validates that a source string maps to a registered provider.
    /// </summary>
    bool TryValidateSource(string source, out string error);

    /// <summary>
    /// Validates an external link ID for format and provider-specific rules.
    /// </summary>
    bool TryValidateId(string source, string id, out string error);
}
