using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Arc API operations
/// </summary>
public interface IArcApiService
{
    /// <summary>
    /// Get all arcs for a campaign
    /// </summary>
    Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId);

    /// <summary>
    /// Get a specific arc
    /// </summary>
    Task<ArcDto?> GetArcAsync(Guid arcId);

    /// <summary>
    /// Create a new arc
    /// </summary>
    Task<ArcDto?> CreateArcAsync(ArcCreateDto dto);

    /// <summary>
    /// Update an arc
    /// </summary>
    Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto);

    /// <summary>
    /// Delete an arc (only if empty)
    /// </summary>
    Task<bool> DeleteArcAsync(Guid arcId);
}
