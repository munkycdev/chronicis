namespace Chronicis.Shared.DTOs;

/// <summary>
/// DTO for hashtag information
/// </summary>
public class HashtagDto
{
    /// <summary>
    /// Hashtag ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Hashtag name (without # symbol, lowercase)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the article this hashtag links to (null if unlinked)
    /// </summary>
    public Guid? LinkedArticleId { get; set; }

    /// <summary>
    /// Title of the linked article (null if unlinked)
    /// </summary>
    public string? LinkedArticleTitle { get; set; }

    /// <summary>
    /// Number of articles that use this hashtag
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// When this hashtag was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for linking a hashtag to an article
/// </summary>
public class LinkHashtagDto
{
    /// <summary>
    /// ID of the article to link to
    /// </summary>
    public Guid ArticleId { get; set; }
}
