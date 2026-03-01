using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IWorldLinkService
{
    Task<ServiceResult<List<WorldLinkDto>>> GetWorldLinksAsync(Guid worldId, Guid userId);
    Task<ServiceResult<WorldLinkDto>> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto, Guid userId);
    Task<ServiceResult<WorldLinkDto>> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto, Guid userId);
    Task<ServiceResult<bool>> DeleteWorldLinkAsync(Guid worldId, Guid linkId, Guid userId);
}

