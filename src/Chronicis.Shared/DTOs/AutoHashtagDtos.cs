namespace Chronicis.Shared.DTOs;

/// <summary>
/// Request to auto-generate hashtags across articles
/// </summary>
public class AutoHashtagRequest
{
    /// <summary>
    /// If true, preview changes without applying them
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Optional: Specific article IDs to process. If null/empty, process all articles.
    /// </summary>
    public List<int>? ArticleIds { get; set; }
}

/// <summary>
/// Response containing auto-hashtag results
/// </summary>
public class AutoHashtagResponse
{
    /// <summary>
    /// List of articles with potential changes
    /// </summary>
    public List<AutoHashtagChange> Changes { get; set; } = new();

    /// <summary>
    /// Total number of articles scanned
    /// </summary>
    public int TotalArticlesScanned { get; set; }

    /// <summary>
    /// Total number of matches found across all articles
    /// </summary>
    public int TotalMatchesFound { get; set; }

    /// <summary>
    /// Whether this was a dry run (preview) or actual application
    /// </summary>
    public bool WasDryRun { get; set; }
}

/// <summary>
/// Details of hashtag changes for a single article
/// </summary>
public class AutoHashtagChange
{
    /// <summary>
    /// Article ID
    /// </summary>
    public int ArticleId { get; set; }

    /// <summary>
    /// Article title
    /// </summary>
    public string ArticleTitle { get; set; } = string.Empty;

    /// <summary>
    /// Number of hashtag matches found
    /// </summary>
    public int MatchesFound { get; set; }

    /// <summary>
    /// Preview of the updated body with hashtags highlighted
    /// Uses <mark> tags to highlight new hashtags
    /// </summary>
    public string PreviewBody { get; set; } = string.Empty;

    /// <summary>
    /// Original body (before changes)
    /// </summary>
    public string OriginalBody { get; set; } = string.Empty;

    /// <summary>
    /// List of article titles that were matched
    /// </summary>
    public List<string> MatchedTitles { get; set; } = new();
}
