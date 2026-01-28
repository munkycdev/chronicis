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
    
    // Aliases
    public List<ArticleAliasDto> Aliases { get; set; } = new();
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
    
    /// <summary>
    /// Indicates whether this article has an AI-generated summary.
    /// Used to display a visual indicator in the navigation tree.
    /// </summary>
    public bool HasAISummary { get; set; }
    
    /// <summary>
    /// True if this is a virtual group (Campaigns, Player Characters, Wiki, etc.)
    /// that doesn't correspond to a real article in the database.
    /// Virtual groups should only expand/collapse, not navigate.
    /// </summary>
    public bool IsVirtualGroup { get; set; } = false;
    
    /// <summary>
    /// Alternative names/aliases for this article.
    /// Used for autocomplete matching and display.
    /// </summary>
    public List<string> Aliases { get; set; } = new();
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
/// Can represent either a World (when IsWorld=true) or an Article.
/// </summary>
public class BreadcrumbDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Article type. Only relevant when IsWorld is false.
    /// </summary>
    public ArticleType Type { get; set; }
    
    /// <summary>
    /// True if this breadcrumb represents a World, false for Articles.
    /// </summary>
    public bool IsWorld { get; set; } = false;
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

/// <summary>
/// DTO for article alias data.
/// </summary>
public class ArticleAliasDto
{
    public Guid Id { get; set; }
    public string AliasText { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional type classification (e.g., "FormerName", "Nickname", "Title").
    /// Reserved for future use.
    /// </summary>
    public string? AliasType { get; set; }
    
    /// <summary>
    /// Optional date when this alias became effective.
    /// Reserved for future use.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for updating all aliases on an article.
/// Accepts a comma-delimited string that will be parsed into individual aliases.
/// </summary>
public class ArticleAliasesUpdateDto
{
    /// <summary>
    /// Comma-delimited list of aliases (e.g., "Icara, The Wanderer, Former Name").
    /// Spaces around commas will be trimmed. Empty entries will be ignored.
    /// </summary>
    public string Aliases { get; set; } = string.Empty;
}
