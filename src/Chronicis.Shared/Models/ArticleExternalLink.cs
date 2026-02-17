using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents an ExternalReference - embedded references to third-party D&D content.
/// Extracted from HTML spans with data-type="external-link".
/// <para>
/// <strong>Vocabulary:</strong> This is an "ExternalReference" - see docs/Vocabulary.md for terminology definitions.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public class ArticleExternalLink
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

    // Navigation properties

    /// <summary>
    /// The article that contains this external link.
    /// </summary>
    public Article Article { get; set; } = null!;
}
