using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for world management
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
    /// Check if user has access to a world
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Check if user owns a world
    /// </summary>
    Task<bool> UserOwnsWorldAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Get a world by its slug for a specific owner
    /// </summary>
    Task<WorldDto?> GetWorldBySlugAsync(string slug, Guid userId);

    /// <summary>
    /// Check if a public slug is available (not already in use)
    /// </summary>
    Task<bool> IsPublicSlugAvailableAsync(string publicSlug, Guid? excludeWorldId = null);

    /// <summary>
    /// Check public slug availability and return detailed result with suggestions
    /// </summary>
    Task<PublicSlugCheckResultDto> CheckPublicSlugAsync(string slug, Guid? excludeWorldId = null);

    /// <summary>
    /// Get a world by its public slug (for anonymous access to public worlds)
    /// </summary>
    Task<WorldDto?> GetWorldByPublicSlugAsync(string publicSlug);
}
