using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Arc API operations
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
        try
        {
            _logger.LogInformation("Fetching arcs for campaign {CampaignId}", campaignId);
            var result = await _http.GetFromJsonAsync<List<ArcDto>>($"api/campaigns/{campaignId}/arcs");
            _logger.LogInformation("Received {Count} arcs", result?.Count ?? 0);
            return result ?? new List<ArcDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching arcs for campaign {CampaignId}", campaignId);
            return new List<ArcDto>();
        }
    }

    public async Task<ArcDto?> GetArcAsync(Guid arcId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ArcDto>($"api/arcs/{arcId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching arc {ArcId}", arcId);
            return null;
        }
    }

    public async Task<ArcDto?> CreateArcAsync(ArcCreateDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/arcs", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ArcDto>();
            }
            
            _logger.LogWarning("Failed to create arc: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating arc");
            return null;
        }
    }

    public async Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/arcs/{arcId}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ArcDto>();
            }
            
            _logger.LogWarning("Failed to update arc: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating arc {ArcId}", arcId);
            return null;
        }
    }

    public async Task<bool> DeleteArcAsync(Guid arcId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/arcs/{arcId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting arc {ArcId}", arcId);
            return false;
        }
    }
}
