using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Campaign API operations
/// </summary>
public interface ICampaignApiService
{
    /// <summary>
    /// Get a specific campaign
    /// </summary>
    Task<CampaignDetailDto?> GetCampaignAsync(Guid campaignId);

    /// <summary>
    /// Create a new campaign
    /// </summary>
    Task<CampaignDto?> CreateCampaignAsync(CampaignCreateDto dto);

    /// <summary>
    /// Update a campaign
    /// </summary>
    Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto);

    /// <summary>
    /// Activate a campaign for quick session creation
    /// </summary>
    Task<bool> ActivateCampaignAsync(Guid campaignId);

    /// <summary>
    /// Get the active context (campaign/arc) for a world
    /// </summary>
    Task<ActiveContextDto?> GetActiveContextAsync(Guid worldId);
}
