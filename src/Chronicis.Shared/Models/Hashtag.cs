namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a hashtag extracted from article content.
/// Hashtags can optionally be linked to a specific article for navigation.
/// </summary>
public class Hashtag
{
    /// <summary>
    /// Unique identifier for the hashtag.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The hashtag name (case-insensitive, stored in lowercase).
    /// e.g., "waterdeep", "vajrasafahr". Max 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The article this hashtag links to.
    /// Null if hashtag is not yet linked.
    /// </summary>
    public Guid? LinkedArticleId { get; set; }

    /// <summary>
    /// When this hashtag was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====
    
    /// <summary>
    /// Navigation property to the linked article.
    /// </summary>
    public Article? LinkedArticle { get; set; }

    /// <summary>
    /// Collection of articles that use this hashtag.
    /// </summary>
    public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();
}
