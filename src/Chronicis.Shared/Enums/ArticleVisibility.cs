namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the visibility of an article.
/// </summary>
public enum ArticleVisibility
{
    /// <summary>
    /// Everyone in the campaign can see this article.
    /// </summary>
    Public = 0,
    
    /// <summary>
    /// Only the owner can see this article. Absolute privacy - no DM override.
    /// </summary>
    Private = 1
}
