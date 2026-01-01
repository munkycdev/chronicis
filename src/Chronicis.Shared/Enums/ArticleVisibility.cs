namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the visibility of an article.
/// Ordered by restriction level: Public (least) → MembersOnly → Private (most).
/// </summary>
public enum ArticleVisibility
{
    /// <summary>
    /// Visible to everyone, including anonymous users on public worlds.
    /// </summary>
    Public = 0,
    
    /// <summary>
    /// Visible only to authenticated world/campaign members.
    /// Hidden from anonymous users viewing public worlds.
    /// </summary>
    MembersOnly = 1,
    
    /// <summary>
    /// Visible only to the article creator. Absolute privacy - no DM override.
    /// </summary>
    Private = 2
}
