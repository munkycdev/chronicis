using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a reusable template for AI summary generation.
/// System templates are built-in and cannot be modified.
/// Future: World-specific templates can be created by users.
/// </summary>
[ExcludeFromCodeCoverage]
public class SummaryTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// World this template belongs to. Null for system templates.
    /// Future: Users can create world-specific templates.
    /// </summary>
    public Guid? WorldId { get; set; }

    /// <summary>
    /// Display name of the template (e.g., "Character", "Location", "Bestiary").
    /// Max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description/help text explaining when to use this template.
    /// Max 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The prompt template text with placeholders.
    /// Supported placeholders: {EntityName}, {EntityType}, {SourceContent}, {WebContent}
    /// </summary>
    public string PromptTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system template (built-in, cannot be deleted/edited).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// User who created this template. Null for system templates.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// When the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====

    /// <summary>
    /// World this template belongs to (for future world-specific templates).
    /// </summary>
    public World? World { get; set; }

    /// <summary>
    /// User who created this template.
    /// </summary>
    public User? Creator { get; set; }
}
