using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Client API service for resolving contextual tutorial content.
/// </summary>
public interface ITutorialApiService
{
    /// <summary>
    /// Resolves the tutorial content for a page type key.
    /// Returns null when no tutorial could be resolved.
    /// </summary>
    Task<TutorialDto?> ResolveAsync(string pageType);
}
