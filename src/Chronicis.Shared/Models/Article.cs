using System.ComponentModel.DataAnnotations.Schema;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.Models;

/// <summary>
/// Core entity representing a hierarchical article/note in Chronicis.
/// Supports infinite nesting through self-referencing ParentId.
/// </summary>
public class Article
{
    /// <summary>
    /// Unique identifier for the article.
    /// </summary>
    public Guid Id { get; set; }
    
    // ===== Hierarchy & Scoping =====
    
    /// <summary>
    /// Parent article ID for hierarchical nesting. Null for root articles.
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// World this article belongs to. Set for Wiki articles, Characters, and root containers.
    /// </summary>
    public Guid? WorldId { get; set; }
    
    /// <summary>
    /// Campaign this article belongs to. Set for campaign-specific articles (Sessions, Acts, etc.).
    /// </summary>
    public Guid? CampaignId { get; set; }
    
    // ===== Content =====
    
    /// <summary>
    /// Article title. Max 500 characters.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly slug derived from title. Max 200 characters.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Article body content (Markdown/HTML).
    /// </summary>
    public string? Body { get; set; }
    
    /// <summary>
    /// Optional emoji icon for the article.
    /// </summary>
    public string? IconEmoji { get; set; }
    
    // ===== Type & Visibility =====
    
    /// <summary>
    /// The type of article, determining behavior and valid parent/child relationships.
    /// </summary>
    public ArticleType Type { get; set; } = ArticleType.WikiArticle;
    
    /// <summary>
    /// Visibility of the article (Public or Private).
    /// </summary>
    public ArticleVisibility Visibility { get; set; } = ArticleVisibility.Public;
    
    // ===== Ownership & Audit =====
    
    /// <summary>
    /// User who created this article.
    /// </summary>
    public Guid CreatedBy { get; set; }
    
    /// <summary>
    /// User who last modified this article. Null if never modified.
    /// </summary>
    public Guid? LastModifiedBy { get; set; }
    
    /// <summary>
    /// When the article was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the article was last modified. Null if never modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    
    // ===== Session-Specific Fields =====
    
    /// <summary>
    /// Real-world date when the session was played. For Session articles only.
    /// </summary>
    public DateTime? SessionDate { get; set; }
    
    /// <summary>
    /// In-game calendar date (flexible format). For Session articles only.
    /// </summary>
    public string? InGameDate { get; set; }
    
    // ===== Character-Specific Fields =====
    
    /// <summary>
    /// User who owns this character. For Character articles only.
    /// </summary>
    public Guid? PlayerId { get; set; }
    
    // ===== AI Features =====
    
    /// <summary>
    /// AI-generated summary of the article.
    /// </summary>
    public string? AISummary { get; set; }
    
    /// <summary>
    /// When the AI summary was last generated.
    /// </summary>
    public DateTime? AISummaryGeneratedAt { get; set; }
    
    // ===== Legacy Fields (for reference during migration) =====
    
    /// <summary>
    /// Effective date for the article content.
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    // ===== Navigation Properties =====
    
    /// <summary>
    /// Parent article in the hierarchy.
    /// </summary>
    public Article? Parent { get; set; }
    
    /// <summary>
    /// Child articles in the hierarchy.
    /// </summary>
    public ICollection<Article>? Children { get; set; }
    
    /// <summary>
    /// World this article belongs to.
    /// </summary>
    public World? World { get; set; }
    
    /// <summary>
    /// Campaign this article belongs to.
    /// </summary>
    public Campaign? Campaign { get; set; }
    
    /// <summary>
    /// User who created this article.
    /// </summary>
    public User Creator { get; set; } = null!;
    
    /// <summary>
    /// User who last modified this article.
    /// </summary>
    public User? Modifier { get; set; }
    
    /// <summary>
    /// User who owns this character (for Character articles).
    /// </summary>
    public User? Player { get; set; }
    
    /// <summary>
    /// Hashtags associated with this article.
    /// </summary>
    public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();

    /// <summary>
    /// Computed property for child count (not mapped to database).
    /// </summary>
    [NotMapped]
    public int ChildCount => Children?.Count ?? 0;
}
