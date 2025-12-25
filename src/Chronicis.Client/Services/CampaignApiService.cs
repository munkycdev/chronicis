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

    public async Task<CampaignMemberDto?> AddMemberAsync(Guid campaignId, CampaignMemberAddDto dto)
    {
        return await _http.PostEntityAsync<CampaignMemberDto>(
            $"api/campaigns/{campaignId}/members",
            dto,
            _logger,
            $"member for campaign {campaignId}");
    }

    public async Task<CampaignMemberDto?> UpdateMemberAsync(Guid campaignId, Guid userId, CampaignMemberUpdateDto dto)
    {
        return await _http.PutEntityAsync<CampaignMemberDto>(
            $"api/campaigns/{campaignId}/members/{userId}",
            dto,
            _logger,
            $"member {userId} in campaign {campaignId}");
    }

    public async Task<bool> RemoveMemberAsync(Guid campaignId, Guid userId)
    {
        return await _http.DeleteEntityAsync(
            $"api/campaigns/{campaignId}/members/{userId}",
            _logger,
            $"member {userId} from campaign {campaignId}");
    }
}
