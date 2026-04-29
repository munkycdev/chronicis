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
    Task<ServiceResult<bool>> DeleteSessionAsync(Guid sessionId, Guid userId);
    Task<ServiceResult<SummaryGenerationDto>> GenerateAiSummaryAsync(Guid sessionId, Guid userId);
    Task<ServiceResult<bool>> ClearAiSummaryAsync(Guid sessionId, Guid userId);

    /// <summary>
    /// Lightweight slug lookup — returns (Id, Name) or null when not found.
    /// Visibility filtering is the caller's responsibility.
    /// </summary>
    Task<(Guid Id, string Name)?> GetIdBySlugAsync(Guid arcId, string slug);

    /// <summary>
    /// Update the session's slug. Validates, checks reserved list, resolves sibling collisions.
    /// </summary>
    Task<ServiceResult<string>> UpdateSlugAsync(Guid sessionId, string slug, Guid userId);
}
