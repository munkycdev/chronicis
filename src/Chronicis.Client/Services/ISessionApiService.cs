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
    /// Create a Session entity under an Arc.
    /// </summary>
    Task<SessionDto?> CreateSessionAsync(Guid arcId, SessionCreateDto dto);

    /// <summary>
    /// Get a Session entity by id.
    /// </summary>
    Task<SessionDto?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Update editable Session fields (notes and metadata).
    /// </summary>
    Task<SessionDto?> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto);

    /// <summary>
    /// Generate AI summary for a Session.
    /// </summary>
    Task<SummaryGenerationDto?> GenerateAiSummaryAsync(Guid sessionId);
}
