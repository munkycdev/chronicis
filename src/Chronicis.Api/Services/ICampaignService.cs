using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for campaign management
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Get a campaign by ID with members
    /// </summary>
    Task<CampaignDetailDto?> GetCampaignAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Create a new campaign with Act 1 and SharedInfoRoot
    /// </summary>
    Task<CampaignDto> CreateCampaignAsync(CampaignCreateDto dto, Guid userId);

    /// <summary>
    /// Update a campaign's details
    /// </summary>
    Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto, Guid userId);

    /// <summary>
    /// Add a member to a campaign
    /// </summary>
    Task<CampaignMemberDto?> AddMemberAsync(Guid campaignId, CampaignMemberAddDto dto, Guid requestingUserId);

    /// <summary>
    /// Update a member's role
    /// </summary>
    Task<CampaignMemberDto?> UpdateMemberAsync(Guid campaignId, Guid userId, CampaignMemberUpdateDto dto, Guid requestingUserId);

    /// <summary>
    /// Remove a member from a campaign
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid campaignId, Guid userId, Guid requestingUserId);

    /// <summary>
    /// Get the user's role in a campaign
    /// </summary>
    Task<CampaignRole?> GetUserRoleAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Check if user has access to a campaign
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Check if user is DM of a campaign
    /// </summary>
    Task<bool> UserIsDungeonMasterAsync(Guid campaignId, Guid userId);
}
