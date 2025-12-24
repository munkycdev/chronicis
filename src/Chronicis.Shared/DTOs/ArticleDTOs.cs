using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Full article with all details and optional children.
/// Used for detailed views, editing, and full CRUD operations.
/// </summary>
public class ArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Guid? WorldId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ArcId { get; set; }
    public string Body { get; set; } = string.Empty;
    public ArticleType Type { get; set; }
    public ArticleVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public Guid? LastModifiedBy { get; set; }
    public string? LastModifiedByName { get; set; }
    public bool HasChildren { get; set; } = false;
    public int ChildCount { get; set; } = 0;
    public ICollection<ArticleDto>? Children { get; set; }
    public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();
    public string? IconEmoji { get; set; }
    
    // Session-specific
    public DateTime? SessionDate { get; set; }
    public string? InGameDate { get; set; }
    
    // Character-specific
    public Guid? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    
    // AI features
    public string? AISummary { get; set; }
    public DateTime? AISummaryGeneratedAt { get; set; }
}

/// <summary>
/// Lightweight DTO for tree view display.
/// Contains only essential fields for efficient navigation rendering.
/// </summary>
public class ArticleTreeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Guid? WorldId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ArcId { get; set; }
    public ArticleType Type { get; set; }
    public ArticleVisibility Visibility { get; set; }
    public bool HasChildren { get; set; }
    public int ChildCount { get; set; } = 0;
    public ICollection<ArticleTreeDto>? Children { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string? IconEmoji { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Request DTO for creating new articles.
/// </summary>
public class ArticleCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? WorldId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ArcId { get; set; }
    public string Body { get; set; } = string.Empty;
    public ArticleType Type { get; set; } = ArticleType.WikiArticle;
    public ArticleVisibility Visibility { get; set; } = ArticleVisibility.Public;

    /// <summary>
    /// Optional effective date. If null, defaults to CreatedAt.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
    
    /// <summary>
    /// Optional emoji icon for the article.
    /// </summary>
    public string? IconEmoji { get; set; }
    
    // Session-specific
    public DateTime? SessionDate { get; set; }
    public string? InGameDate { get; set; }
    
    // Character-specific
    public Guid? PlayerId { get; set; }
}

/// <summary>
/// Request DTO for updating existing articles.
/// </summary>
public class ArticleUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public string? IconEmoji { get; set; }
    public ArticleVisibility? Visibility { get; set; }
    
    /// <summary>
    /// Optional type change. Allows users to recategorize articles.
    /// </summary>
    public ArticleType? Type { get; set; }
    
    // Session-specific
    public DateTime? SessionDate { get; set; }
    public string? InGameDate { get; set; }
}

/// <summary>
/// Breadcrumb item for navigation path display.
/// </summary>
public class BreadcrumbDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ArticleType Type { get; set; }
}

/// <summary>
/// Request DTO for moving an article to a new parent.
/// </summary>
public class ArticleMoveDto
{
    /// <summary>
    /// The new parent article ID. Set to null to move the article to root level.
    /// </summary>
    public Guid? NewParentId { get; set; }
}
