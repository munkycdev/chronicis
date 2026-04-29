using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for campaign management
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Get a campaign by ID
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
    /// Get the user's role in a campaign's world
    /// </summary>
    Task<WorldRole?> GetUserRoleAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Check if user has access to a campaign (via world membership)
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Check if user is GM of a campaign's world
    /// </summary>
    Task<bool> UserIsGMAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Set a campaign as active (deactivates others in same world)
    /// </summary>
    Task<bool> ActivateCampaignAsync(Guid campaignId, Guid userId);

    /// <summary>
    /// Get the active context (campaign/arc) for a world, or infer if only one exists
    /// </summary>
    Task<ActiveContextDto> GetActiveContextAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Lightweight slug lookup — returns (Id, Name) or null when not found.
    /// Visibility filtering is the caller's responsibility.
    /// </summary>
    Task<(Guid Id, string Name)?> GetIdBySlugAsync(Guid worldId, string slug);

    /// <summary>
    /// Update the campaign's slug. Validates, checks reserved list, resolves sibling collisions.
    /// </summary>
    Task<ServiceResult<string>> UpdateSlugAsync(Guid campaignId, string slug, Guid userId);
}
