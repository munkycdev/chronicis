using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for core world management (CRUD, lookup, creation)
/// </summary>
public interface IWorldService
{
    /// <summary>
    /// Get all worlds the user has access to (owned or member of a campaign)
    /// </summary>
    Task<List<WorldDto>> GetUserWorldsAsync(Guid userId);

    /// <summary>
    /// Get a world by ID with campaign list
    /// </summary>
    Task<WorldDetailDto?> GetWorldAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Create a new world with root structure
    /// </summary>
    Task<WorldDto> CreateWorldAsync(WorldCreateDto dto, Guid userId);

    /// <summary>
    /// Update a world's name, description, and public visibility
    /// </summary>
    Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto, Guid userId);

    /// <summary>
    /// Get a world by its slug for a specific owner
    /// </summary>
    Task<WorldDto?> GetWorldBySlugAsync(string slug, Guid userId);
}
