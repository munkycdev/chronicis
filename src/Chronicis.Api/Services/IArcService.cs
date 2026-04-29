using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArcService
{
    Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId, Guid userId);
    Task<ArcDto?> GetArcAsync(Guid arcId, Guid userId);
    Task<ArcDto?> CreateArcAsync(ArcCreateDto dto, Guid userId);
    Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto, Guid userId);
    Task<bool> DeleteArcAsync(Guid arcId, Guid userId);
    Task<bool> ActivateArcAsync(Guid arcId, Guid userId);

    /// <summary>
    /// Lightweight slug lookup — returns (Id, Name) or null when not found.
    /// Visibility filtering is the caller's responsibility.
    /// </summary>
    Task<(Guid Id, string Name)?> GetIdBySlugAsync(Guid campaignId, string slug);

    /// <summary>
    /// Update the arc's slug. Validates, checks reserved list, resolves sibling collisions.
    /// </summary>
    Task<ServiceResult<string>> UpdateSlugAsync(Guid arcId, string slug, Guid userId);
}
