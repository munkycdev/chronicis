using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Maps an application page context key to a tutorial article.
/// </summary>
[ExcludeFromCodeCoverage]
public class TutorialPage
{
    public Guid Id { get; set; }

    /// <summary>
    /// Canonical page context key (for example, "Page:Dashboard").
    /// </summary>
    public string PageType { get; set; } = string.Empty;

    /// <summary>
    /// Human-friendly label shown in admin UI.
    /// </summary>
    public string PageTypeName { get; set; } = string.Empty;

    public Guid ArticleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public Article Article { get; set; } = null!;
}
