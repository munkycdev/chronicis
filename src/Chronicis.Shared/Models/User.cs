namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a user authenticated via Auth0.
/// Each user has a unique Auth0 user ID and can create their own articles.
/// </summary>
public class User
{
    /// <summary>
    /// Internal database ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Auth0 user ID (e.g., "google-oauth2|123456" or "discord|789012")
    /// This is unique across all identity providers
    /// </summary>
    public string Auth0UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address from Auth0
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name from Auth0 (could be username, real name, etc.)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Avatar/profile picture URL from Auth0
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// When the user first created their account in Chronicis
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user logged in
    /// </summary>
    public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: All articles created by this user
    /// </summary>
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
