using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Interface for managing resource providers via the API.
/// </summary>
public interface IResourceProviderApiService
{
    /// <summary>
    /// Gets all resource providers with their enabled status for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>List of providers with enabled status</returns>
    Task<List<WorldResourceProviderDto>?> GetWorldProvidersAsync(Guid worldId);

    /// <summary>
    /// Enables or disables a resource provider for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <param name="providerCode">The provider code</param>
    /// <param name="enabled">Whether to enable or disable</param>
    /// <param name="lookupKey">
    /// Optional lookup key update. Null means "leave existing value unchanged".
    /// Empty/whitespace resets to default (provider code).
    /// </param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ToggleProviderAsync(Guid worldId, string providerCode, bool enabled, string? lookupKey = null);
}
