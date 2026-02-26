using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// API client for system administrator operations.
/// </summary>
public interface IAdminApiService
{
    /// <summary>
    /// Returns all worlds in the system with aggregate counts.
    /// Returns an empty list on failure or authorization error.
    /// </summary>
    Task<List<AdminWorldSummaryDto>> GetWorldSummariesAsync();

    /// <summary>
    /// Permanently deletes a world and all of its associated data.
    /// Returns true on success, false if the world was not found or the call failed.
    /// </summary>
    Task<bool> DeleteWorldAsync(Guid worldId);

    /// <summary>
    /// Returns all tutorial page mappings for SysAdmin management.
    /// Returns an empty list on failure or authorization error.
    /// </summary>
    Task<List<TutorialMappingDto>> GetTutorialMappingsAsync();

    /// <summary>
    /// Creates a new tutorial article + mapping for a page type.
    /// Returns null on failure.
    /// </summary>
    Task<TutorialMappingDto?> CreateTutorialMappingAsync(TutorialMappingCreateDto dto);
}
