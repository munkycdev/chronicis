using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

// ====================================================================================
// WorldBookmark DTOs
// These DTOs represent WorldBookmarks (user-saved external URLs for campaigns).
// Vocabulary: See docs/Vocabulary.md for terminology definitions.
// ====================================================================================

/// <summary>
/// DTO for reading a WorldBookmark.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldLinkDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new WorldBookmark.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldLinkCreateDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing WorldBookmark.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldLinkUpdateDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
