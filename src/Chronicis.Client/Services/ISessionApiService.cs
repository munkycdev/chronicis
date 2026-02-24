using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Session API operations used by the client tree/navigation.
/// </summary>
public interface ISessionApiService
{
    /// <summary>
    /// Get all Session entities for an Arc.
    /// </summary>
    Task<List<SessionTreeDto>> GetSessionsByArcAsync(Guid arcId);

    /// <summary>
    /// Get a Session entity by id.
    /// </summary>
    Task<SessionDto?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Update Session public/private notes.
    /// </summary>
    Task<SessionDto?> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto);

    /// <summary>
    /// Generate AI summary for a Session.
    /// </summary>
    Task<SummaryGenerationDto?> GenerateAiSummaryAsync(Guid sessionId);
}
