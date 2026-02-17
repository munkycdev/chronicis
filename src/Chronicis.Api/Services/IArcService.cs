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
}
