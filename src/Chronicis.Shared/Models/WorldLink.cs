using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a WorldBookmark - user-saved external URLs associated with a campaign.
/// Examples: Roll20 campaign, D&D Beyond, Discord servers, homebrew wikis, etc.
/// <para>
/// <strong>Vocabulary:</strong> This is a "WorldBookmark" - see docs/Vocabulary.md for terminology definitions.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldLink
{
    /// <summary>
    /// Unique identifier for the link.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The world this link belongs to.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// The URL of the external resource.
    /// Max 2048 characters.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the link (e.g., "Roll20 Campaign", "D&D Beyond").
    /// Max 200 characters.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this link is for.
    /// Max 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the link was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====

    /// <summary>
    /// The world this link belongs to.
    /// </summary>
    public World World { get; set; } = null!;
}
