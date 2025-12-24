using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for World API operations
/// </summary>
public class WorldApiService : IWorldApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<WorldApiService> _logger;

    public WorldApiService(HttpClient http, ILogger<WorldApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<WorldDto>> GetWorldsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching worlds from API");
            var result = await _http.GetFromJsonAsync<List<WorldDto>>("api/worlds");
            _logger.LogInformation("Received {Count} worlds", result?.Count ?? 0);
            return result ?? new List<WorldDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching worlds");
            return new List<WorldDto>();
        }
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId)
    {
        try
        {
            return await _http.GetFromJsonAsync<WorldDetailDto>($"api/worlds/{worldId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching world {WorldId}", worldId);
            return null;
        }
    }

    public async Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/worlds", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WorldDto>();
            }
            
            _logger.LogWarning("Failed to create world: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating world");
            return null;
        }
    }

    public async Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/worlds/{worldId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WorldDto>();
            }
            
            _logger.LogWarning("Failed to update world: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating world {WorldId}", worldId);
            return null;
        }
    }

    // ===== World Links =====

    public async Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<WorldLinkDto>>($"api/worlds/{worldId}/links");
            return result ?? new List<WorldLinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching links for world {WorldId}", worldId);
            return new List<WorldLinkDto>();
        }
    }

    public async Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/worlds/{worldId}/links", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WorldLinkDto>();
            }
            
            _logger.LogWarning("Failed to create world link: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating link for world {WorldId}", worldId);
            return null;
        }
    }

    public async Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/worlds/{worldId}/links/{linkId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WorldLinkDto>();
            }
            
            _logger.LogWarning("Failed to update world link: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating link {LinkId} for world {WorldId}", linkId, worldId);
            return null;
        }
    }

    public async Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/worlds/{worldId}/links/{linkId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting link {LinkId} for world {WorldId}", linkId, worldId);
            return false;
        }
    }
}
