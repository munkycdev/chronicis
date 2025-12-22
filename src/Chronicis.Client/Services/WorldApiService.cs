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
}
