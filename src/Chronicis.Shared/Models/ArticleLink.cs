namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a WikiLink - internal article-to-article references within a campaign.
/// Links use [[ArticleName]] or [[ArticleName|Display Text]] syntax.
/// <para>
/// <strong>Vocabulary:</strong> This is a "WikiLink" - see docs/Vocabulary.md for terminology definitions.
/// </para>
/// </summary>
public class ArticleLink
{
    /// <summary>
    /// Unique identifier for this link.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The article containing this link.
    /// </summary>
    public Guid SourceArticleId { get; set; }

    /// <summary>
    /// The article being linked to.
    /// </summary>
    public Guid TargetArticleId { get; set; }

    /// <summary>
    /// Custom display text for the link.
    /// Null means use the target article's title.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Position in the source article body (character offset).
    /// Used for ordering in backlinks display.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// When this link was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Article SourceArticle { get; set; } = null!;
    public Article TargetArticle { get; set; } = null!;
}
