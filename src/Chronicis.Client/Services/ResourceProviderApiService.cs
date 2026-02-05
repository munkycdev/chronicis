using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing resource providers via the API.
/// </summary>
public class ResourceProviderApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResourceProviderApiService> _logger;

    public ResourceProviderApiService(
        HttpClient httpClient,
        ILogger<ResourceProviderApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets all resource providers with their enabled status for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>List of providers with enabled status</returns>
    public async Task<List<WorldResourceProviderDto>?> GetWorldProvidersAsync(Guid worldId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/worlds/{worldId}/resource-providers");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<WorldResourceProviderDto>>();
            }

            _logger.LogWarning(
                "Failed to get providers for world {WorldId}. Status: {StatusCode}",
                worldId,
                response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting providers for world {WorldId}", worldId);
            return null;
        }
    }

    /// <summary>
    /// Enables or disables a resource provider for a specific world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <param name="providerCode">The provider code</param>
    /// <param name="enabled">Whether to enable or disable</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> ToggleProviderAsync(Guid worldId, string providerCode, bool enabled)
    {
        try
        {
            var request = new ToggleResourceProviderRequest { Enabled = enabled };
            var response = await _httpClient.PostAsJsonAsync(
                $"api/worlds/{worldId}/resource-providers/{providerCode}/toggle",
                request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Successfully {Action} provider {ProviderCode} for world {WorldId}",
                    enabled ? "enabled" : "disabled",
                    providerCode,
                    worldId);
                return true;
            }

            _logger.LogWarning(
                "Failed to toggle provider {ProviderCode} for world {WorldId}. Status: {StatusCode}",
                providerCode,
                worldId,
                response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error toggling provider {ProviderCode} for world {WorldId}",
                providerCode,
                worldId);
            return false;
        }
    }
}
