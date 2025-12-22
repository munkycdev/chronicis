using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Campaign API operations
/// </summary>
public interface ICampaignApiService
{
    /// <summary>
    /// Get a specific campaign with its members
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
    /// Add a member to a campaign
    /// </summary>
    Task<CampaignMemberDto?> AddMemberAsync(Guid campaignId, CampaignMemberAddDto dto);

    /// <summary>
    /// Update a campaign member's role
    /// </summary>
    Task<CampaignMemberDto?> UpdateMemberAsync(Guid campaignId, Guid userId, CampaignMemberUpdateDto dto);

    /// <summary>
    /// Remove a member from a campaign
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid campaignId, Guid userId);
}
