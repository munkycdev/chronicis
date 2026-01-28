namespace Chronicis.Shared.Models;

/// <summary>
/// Represents an alternative name/alias for an article.
/// Enables linking via multiple names (e.g., "Icara" and "Icarax" for the same character).
/// </summary>
public class ArticleAlias
{
    /// <summary>
    /// Unique identifier for the alias.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The article this alias belongs to.
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// The alias text (alternative name for the article).
    /// </summary>
    public string AliasText { get; set; } = string.Empty;

    /// <summary>
    /// Optional type classification for the alias (e.g., FormerName, Nickname, Title).
    /// Reserved for future use.
    /// </summary>
    public string? AliasType { get; set; }

    /// <summary>
    /// Optional date when this alias became effective (e.g., when a character changed names).
    /// Reserved for future use.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// When this alias was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====

    /// <summary>
    /// The article this alias belongs to.
    /// </summary>
    public Article Article { get; set; } = null!;
}
