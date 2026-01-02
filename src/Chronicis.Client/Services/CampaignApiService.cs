using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Campaign API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class CampaignApiService : ICampaignApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<CampaignApiService> _logger;

    public CampaignApiService(HttpClient http, ILogger<CampaignApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<CampaignDetailDto?> GetCampaignAsync(Guid campaignId)
    {
        return await _http.GetEntityAsync<CampaignDetailDto>(
            $"api/campaigns/{campaignId}",
            _logger,
            $"campaign {campaignId}");
    }

    public async Task<CampaignDto?> CreateCampaignAsync(CampaignCreateDto dto)
    {
        return await _http.PostEntityAsync<CampaignDto>(
            "api/campaigns",
            dto,
            _logger,
            "campaign");
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto)
    {
        return await _http.PutEntityAsync<CampaignDto>(
            $"api/campaigns/{campaignId}",
            dto,
            _logger,
            $"campaign {campaignId}");
    }

    public async Task<bool> ActivateCampaignAsync(Guid campaignId)
    {
        try
        {
            var response = await _http.PostAsync($"api/campaigns/{campaignId}/activate", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating campaign {CampaignId}", campaignId);
            return false;
        }
    }

    public async Task<ActiveContextDto?> GetActiveContextAsync(Guid worldId)
    {
        return await _http.GetEntityAsync<ActiveContextDto>(
            $"api/worlds/{worldId}/active-context",
            _logger,
            $"active context for world {worldId}");
    }
}
