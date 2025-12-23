using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Response containing link suggestions for autocomplete.
/// </summary>
public class LinkSuggestionsResponseDto
{
    public List<LinkSuggestionDto> Suggestions { get; set; } = new();
}

/// <summary>
/// A single article suggestion for wiki link autocomplete.
/// </summary>
public class LinkSuggestionDto
{
    /// <summary>
    /// The article ID to link to.
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// The article's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Display path with first level stripped (e.g., "Sword Coast / Waterdeep").
    /// </summary>
    public string DisplayPath { get; set; } = string.Empty;

    /// <summary>
    /// The type of article (for filtering/icons).
    /// </summary>
    public ArticleType ArticleType { get; set; }

    /// <summary>
    /// The article's slug for navigation.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
}

/// <summary>
/// Response containing backlinks to an article.
/// </summary>
public class BacklinksResponseDto
{
    public List<BacklinkDto> Backlinks { get; set; } = new();
}

/// <summary>
/// An article that links to the current article.
/// </summary>
public class BacklinkDto
{
    /// <summary>
    /// The ID of the article containing the link.
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// The title of the linking article.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Display path of the linking article.
    /// </summary>
    public string DisplayPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional snippet showing context around the link.
    /// </summary>
    public string? Snippet { get; set; }

    /// <summary>
    /// The slug for navigation.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
}

/// <summary>
/// Request to resolve multiple article links at once.
/// </summary>
public class LinkResolutionRequestDto
{
    public List<Guid> ArticleIds { get; set; } = new();
}

/// <summary>
/// Response containing resolution status for requested articles.
/// </summary>
public class LinkResolutionResponseDto
{
    /// <summary>
    /// Dictionary mapping article ID to its resolution info.
    /// </summary>
    public Dictionary<Guid, ResolvedLinkDto> Articles { get; set; } = new();
}

/// <summary>
/// Information about whether a linked article exists.
/// </summary>
public class ResolvedLinkDto
{
    /// <summary>
    /// The article ID that was checked.
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Whether the article exists in the database.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// The article's title (if it exists).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The article's slug for navigation (if it exists).
    /// </summary>
    public string? Slug { get; set; }
}

/// <summary>
/// Request to auto-link content in an article.
/// </summary>
public class AutoLinkRequestDto
{
    /// <summary>
    /// The current body content to scan for linkable text.
    /// </summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Response from auto-link operation.
/// </summary>
public class AutoLinkResponseDto
{
    /// <summary>
    /// Number of new links that were found.
    /// </summary>
    public int LinksFound { get; set; }

    /// <summary>
    /// The modified body content with wiki links inserted.
    /// </summary>
    public string ModifiedBody { get; set; } = string.Empty;

    /// <summary>
    /// Details about each link that was found.
    /// </summary>
    public List<AutoLinkMatchDto> Matches { get; set; } = new();
}

/// <summary>
/// Information about a single auto-link match.
/// </summary>
public class AutoLinkMatchDto
{
    /// <summary>
    /// The text that was matched.
    /// </summary>
    public string MatchedText { get; set; } = string.Empty;

    /// <summary>
    /// The article that will be linked to.
    /// </summary>
    public string ArticleTitle { get; set; } = string.Empty;

    /// <summary>
    /// The article ID that will be linked to.
    /// </summary>
    public Guid ArticleId { get; set; }
}
