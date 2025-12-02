using System.ComponentModel.DataAnnotations.Schema;

namespace Chronicis.Shared.Models;

/// <summary>
/// Core entity representing a hierarchical article/note in Chronicis.
/// Supports infinite nesting through self-referencing ParentId.
/// </summary>
public class Article
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Body { get; set; }
    public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string? IconEmoji { get; set; }

    public Article? Parent { get; set; }
    public ICollection<Article>? Children { get; set; }

    [NotMapped]
    public int ChildCount => Children?.Count ?? 0;

    public string? AISummary { get; set; }
    public DateTime? AISummaryGeneratedDate { get; set; }

    public User User { get; set; } = null!;
}
