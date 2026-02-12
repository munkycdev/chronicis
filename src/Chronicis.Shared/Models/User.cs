namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a user authenticated via Auth0.
/// Each user has a unique Auth0 user ID and can own worlds, campaigns, and articles.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Auth0 user ID (e.g., "google-oauth2|123456" or "discord|789012").
    /// This is unique across all identity providers.
    /// </summary>
    public string Auth0UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address from Auth0.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name from Auth0 (could be username, real name, etc.).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Avatar/profile picture URL from Auth0.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// When the user first created their account in Chronicis.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user logged in.
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user has completed the onboarding flow.
    /// New users are redirected to onboarding until this is true.
    /// </summary>
    public bool HasCompletedOnboarding { get; set; } = false;

    // ===== Navigation Properties =====

    /// <summary>
    /// All worlds owned by this user.
    /// </summary>
    public ICollection<World> OwnedWorlds { get; set; } = new List<World>();

    /// <summary>
    /// All campaigns owned by this user (where they are DM).
    /// </summary>
    public ICollection<Campaign> OwnedCampaigns { get; set; } = new List<Campaign>();

    /// <summary>
    /// All world memberships for this user.
    /// </summary>
    public ICollection<WorldMember> WorldMemberships { get; set; } = new List<WorldMember>();

    /// <summary>
    /// All users this user has invited to worlds.
    /// </summary>
    public ICollection<WorldMember> InvitedMembers { get; set; } = new List<WorldMember>();

    /// <summary>
    /// All world invitations created by this user.
    /// </summary>
    public ICollection<WorldInvitation> CreatedInvitations { get; set; } = new List<WorldInvitation>();

    /// <summary>
    /// All arcs created by this user.
    /// </summary>
    public ICollection<Arc> CreatedArcs { get; set; } = new List<Arc>();

    /// <summary>
    /// All articles created by this user.
    /// </summary>
    public ICollection<Article> CreatedArticles { get; set; } = new List<Article>();

    /// <summary>
    /// All articles last modified by this user.
    /// </summary>
    public ICollection<Article> ModifiedArticles { get; set; } = new List<Article>();

    /// <summary>
    /// All characters owned by this user (where they are the player).
    /// </summary>
    public ICollection<Article> OwnedCharacters { get; set; } = new List<Article>();

    /// <summary>
    /// All documents uploaded by this user.
    /// </summary>
    public ICollection<WorldDocument> UploadedDocuments { get; set; } = new List<WorldDocument>();

    /// <summary>
    /// All quests created by this user.
    /// </summary>
    public ICollection<Quest> CreatedQuests { get; set; } = new List<Quest>();

    /// <summary>
    /// All quest updates created by this user.
    /// </summary>
    public ICollection<QuestUpdate> CreatedQuestUpdates { get; set; } = new List<QuestUpdate>();
}
