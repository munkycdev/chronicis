using Chronicis.Shared.Models;

namespace Chronicis.Api.Repositories;

/// <summary>
/// Repository for managing resource providers and their world associations.
/// </summary>
public interface IResourceProviderRepository
{
    /// <summary>
    /// Gets all available resource providers in the system.
    /// </summary>
    /// <returns>List of all resource providers</returns>
    Task<List<ResourceProvider>> GetAllProvidersAsync();

    /// <summary>
    /// Gets all resource providers with their enabled status for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>List of providers with enabled status</returns>
    Task<List<(ResourceProvider Provider, bool IsEnabled, string LookupKey)>> GetWorldProvidersAsync(Guid worldId);

    /// <summary>
    /// Enables or disables a resource provider for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <param name="providerCode">The provider code</param>
    /// <param name="enabled">Whether to enable or disable</param>
    /// <param name="userId">The user making the change</param>
    /// <param name="lookupKey">
    /// Optional lookup key update. Null means "leave existing value unchanged".
    /// Empty/whitespace resets to default (provider code).
    /// </param>
    /// <returns>True if successful, false if provider not found</returns>
    Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId, string? lookupKey = null);
}
