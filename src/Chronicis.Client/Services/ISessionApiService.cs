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
}
