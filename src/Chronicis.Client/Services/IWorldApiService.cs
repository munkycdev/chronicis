using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for World API operations
/// </summary>
public interface IWorldApiService
{
    /// <summary>
    /// Get all worlds the user has access to
    /// </summary>
    Task<List<WorldDto>> GetWorldsAsync();

    /// <summary>
    /// Get a specific world with its campaigns
    /// </summary>
    Task<WorldDetailDto?> GetWorldAsync(Guid worldId);

    /// <summary>
    /// Create a new world
    /// </summary>
    Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto);

    /// <summary>
    /// Update a world
    /// </summary>
    Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto);

    // ===== World Links =====

    /// <summary>
    /// Get all links for a world (sorted alphabetically)
    /// </summary>
    Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId);

    /// <summary>
    /// Create a new link for a world
    /// </summary>
    Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto);

    /// <summary>
    /// Update an existing world link
    /// </summary>
    Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto);

    /// <summary>
    /// Delete a world link
    /// </summary>
    Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId);

    // ===== Public Sharing =====

    /// <summary>
    /// Check if a public slug is available for a world
    /// </summary>
    Task<PublicSlugCheckResultDto?> CheckPublicSlugAsync(Guid worldId, string slug);
}
