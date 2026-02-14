using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

// ====================================================================================
// ExternalReference DTO
// This DTO represents an ExternalReference (embedded third-party D&D content reference).
// Vocabulary: See docs/Vocabulary.md for terminology definitions.
// ====================================================================================

/// <summary>
/// DTO representing an ExternalReference for API responses.
/// Used for embedded references to third-party D&D content (SRD, Open5e, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
public class ArticleExternalLinkDto
{
    /// <summary>
    /// Unique identifier for this external link reference.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The article containing this external link.
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Source system for the external resource (e.g., "srd14", "open5e").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// External resource identifier within the source system (e.g., "classes/the-fiend").
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the external resource (e.g., "The Fiend").
    /// </summary>
    public string DisplayTitle { get; set; } = string.Empty;
}
