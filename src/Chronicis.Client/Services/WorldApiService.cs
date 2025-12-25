using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for World API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
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
        return await _http.GetListAsync<WorldDto>(
            "api/worlds",
            _logger,
            "worlds");
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId)
    {
        return await _http.GetEntityAsync<WorldDetailDto>(
            $"api/worlds/{worldId}",
            _logger,
            $"world {worldId}");
    }

    public async Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldDto>(
            "api/worlds",
            dto,
            _logger,
            "world");
    }

    public async Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldDto>(
            $"api/worlds/{worldId}",
            dto,
            _logger,
            $"world {worldId}");
    }

    // ===== World Links =====

    public async Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links",
            _logger,
            $"links for world {worldId}");
    }

    public async Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links",
            dto,
            _logger,
            $"link for world {worldId}");
    }

    public async Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links/{linkId}",
            dto,
            _logger,
            $"link {linkId} for world {worldId}");
    }

    public async Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId)
    {
        return await _http.DeleteEntityAsync(
            $"api/worlds/{worldId}/links/{linkId}",
            _logger,
            $"link {linkId} from world {worldId}");
    }
}
