using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing resource providers and their world associations.
/// Includes authorization checks and business logic.
/// </summary>
public interface IResourceProviderService
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
    /// <param name="userId">The user requesting the information</param>
    /// <returns>List of providers with enabled status</returns>
    /// <exception cref="UnauthorizedAccessException">User does not have access to this world</exception>
    /// <exception cref="KeyNotFoundException">World not found</exception>
    Task<List<(ResourceProvider Provider, bool IsEnabled, string LookupKey)>> GetWorldProvidersAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Enables or disables a resource provider for a specific world.
    /// Only the world owner can modify provider settings.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <param name="providerCode">The provider code</param>
    /// <param name="enabled">Whether to enable or disable</param>
    /// <param name="lookupKey">
    /// Optional lookup key update. Null means "leave existing value unchanged".
    /// Empty/whitespace resets to default (provider code).
    /// </param>
    /// <param name="userId">The user making the change</param>
    /// <returns>True if successful</returns>
    /// <exception cref="UnauthorizedAccessException">User is not the world owner</exception>
    /// <exception cref="KeyNotFoundException">World or provider not found</exception>
    Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId, string? lookupKey = null);
}
