using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Arc API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class ArcApiService : IArcApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ArcApiService> _logger;

    public ArcApiService(HttpClient http, ILogger<ArcApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId)
    {
        return await _http.GetListAsync<ArcDto>(
            $"api/campaigns/{campaignId}/arcs",
            _logger,
            $"arcs for campaign {campaignId}");
    }

    public async Task<ArcDto?> GetArcAsync(Guid arcId)
    {
        return await _http.GetEntityAsync<ArcDto>(
            $"api/arcs/{arcId}",
            _logger,
            $"arc {arcId}");
    }

    public async Task<ArcDto?> CreateArcAsync(ArcCreateDto dto)
    {
        return await _http.PostEntityAsync<ArcDto>(
            "api/arcs",
            dto,
            _logger,
            "arc");
    }

    public async Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto)
    {
        return await _http.PutEntityAsync<ArcDto>(
            $"api/arcs/{arcId}",
            dto,
            _logger,
            $"arc {arcId}");
    }

    public async Task<bool> DeleteArcAsync(Guid arcId)
    {
        return await _http.DeleteEntityAsync(
            $"api/arcs/{arcId}",
            _logger,
            $"arc {arcId}");
    }
}
