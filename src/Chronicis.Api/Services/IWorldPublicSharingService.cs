using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for world public sharing and slug management
/// </summary>
public interface IWorldPublicSharingService
{
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

    /// <summary>
    /// Validate a public slug format.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    string? ValidatePublicSlug(string slug);
}
