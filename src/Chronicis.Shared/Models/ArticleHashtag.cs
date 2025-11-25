namespace Chronicis.Shared.Models;

/// <summary>
/// Junction table representing the many-to-many relationship between Articles and Hashtags.
/// Tracks which hashtags appear in which articles, including position information.
/// </summary>
public class ArticleHashtag
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Article
    /// </summary>
    public int ArticleId { get; set; }

    /// <summary>
    /// Navigation property to Article
    /// </summary>
    public Article Article { get; set; } = null!;

    /// <summary>
    /// Foreign key to Hashtag
    /// </summary>
    public int HashtagId { get; set; }

    /// <summary>
    /// Navigation property to Hashtag
    /// </summary>
    public Hashtag Hashtag { get; set; } = null!;

    /// <summary>
    /// Position of this hashtag in the article text (0-based character index)
    /// Used for potential future features like "jump to hashtag" or highlighting
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// When this relationship was created (when hashtag was added to article)
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
