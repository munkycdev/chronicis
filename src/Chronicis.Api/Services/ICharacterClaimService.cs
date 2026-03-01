using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface ICharacterClaimService
{
    Task<List<ClaimedCharacterDto>> GetClaimedCharactersAsync(Guid userId);
    Task<(bool Found, Guid? PlayerId, string? PlayerName)> GetClaimStatusAsync(Guid characterId);
    Task<ServiceResult<bool>> ClaimCharacterAsync(Guid characterId, Guid userId);
    Task<ServiceResult<bool>> UnclaimCharacterAsync(Guid characterId, Guid userId);
}

