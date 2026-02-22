using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for system administrator operations.
/// All methods enforce sysadmin authorization before executing.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Returns a summary of every world in the system, with aggregate counts.
    /// Throws <see cref="UnauthorizedAccessException"/> if the caller is not a sysadmin.
    /// </summary>
    Task<List<AdminWorldSummaryDto>> GetAllWorldSummariesAsync();

    /// <summary>
    /// Permanently deletes a world and all of its associated data.
    /// Throws <see cref="UnauthorizedAccessException"/> if the caller is not a sysadmin.
    /// Returns false if the world was not found.
    /// </summary>
    Task<bool> DeleteWorldAsync(Guid worldId);
}
