using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Campaign API operations
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
        try
        {
            return await _http.GetFromJsonAsync<CampaignDetailDto>($"api/campaigns/{campaignId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching campaign {CampaignId}", campaignId);
            return null;
        }
    }

    public async Task<CampaignDto?> CreateCampaignAsync(CampaignCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/campaigns", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CampaignDto>();
            }
            
            _logger.LogWarning("Failed to create campaign: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return null;
        }
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/campaigns/{campaignId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CampaignDto>();
            }
            
            _logger.LogWarning("Failed to update campaign: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", campaignId);
            return null;
        }
    }

    public async Task<CampaignMemberDto?> AddMemberAsync(Guid campaignId, CampaignMemberAddDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/campaigns/{campaignId}/members", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CampaignMemberDto>();
            }
            
            _logger.LogWarning("Failed to add member: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to campaign {CampaignId}", campaignId);
            return null;
        }
    }

    public async Task<CampaignMemberDto?> UpdateMemberAsync(Guid campaignId, Guid userId, CampaignMemberUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/campaigns/{campaignId}/members/{userId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CampaignMemberDto>();
            }
            
            _logger.LogWarning("Failed to update member: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member {UserId} in campaign {CampaignId}", userId, campaignId);
            return null;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid campaignId, Guid userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/campaigns/{campaignId}/members/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {UserId} from campaign {CampaignId}", userId, campaignId);
            return false;
        }
    }
}
