using Chronicis.Api.Models;
using Chronicis.Shared.DTOs.Sessions;

namespace Chronicis.Api.Services;

public interface ISessionService
{
    Task<ServiceResult<SessionDto>> CreateSessionAsync(Guid arcId, SessionCreateDto dto, Guid userId, string? username);
    Task<ServiceResult<SessionDto>> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto, Guid userId);
}
