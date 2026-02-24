using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;

namespace Chronicis.Api.Services;

public interface ISessionService
{
    Task<ServiceResult<List<SessionTreeDto>>> GetSessionsByArcAsync(Guid arcId, Guid userId);
    Task<ServiceResult<SessionDto>> GetSessionAsync(Guid sessionId, Guid userId);
    Task<ServiceResult<SessionDto>> CreateSessionAsync(Guid arcId, SessionCreateDto dto, Guid userId, string? username);
    Task<ServiceResult<SessionDto>> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto, Guid userId);
    Task<ServiceResult<SummaryGenerationDto>> GenerateAiSummaryAsync(Guid sessionId, Guid userId);
}
