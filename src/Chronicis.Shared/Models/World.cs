namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a World - the top-level isolation boundary for content.
/// Each World has its own Wiki, Campaigns, and Characters.
/// DMs create separate Worlds for different settings (e.g., "Forgotten Realms" vs "Eberron").
/// </summary>
public class World
{
    /// <summary>
    /// Unique identifier for the world.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Display name of the world (e.g., "Forgotten Realms", "My Homebrew World").
    /// Max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly slug derived from name (e.g., "forgotten-realms").
    /// Max 200 characters. Unique per owner.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the world.
    /// Max 1000 characters.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The user who owns this world (typically the DM).
    /// </summary>
    public Guid OwnerId { get; set; }
    
    /// <summary>
    /// When the world was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ===== Public Access =====
    
    /// <summary>
    /// Whether this world is publicly accessible to anonymous users.
    /// When true, articles with Public visibility can be viewed without authentication.
    /// </summary>
    public bool IsPublic { get; set; } = false;
    
    /// <summary>
    /// Globally unique URL-friendly slug for public access (e.g., "forgotten-realms").
    /// Only set when IsPublic is true. Max 100 characters.
    /// Lowercase alphanumeric with hyphens only, no leading/trailing hyphens.
    /// </summary>
    public string? PublicSlug { get; set; }
    
    // ===== Navigation Properties =====
    
    /// <summary>
    /// The user who owns this world.
    /// </summary>
    public User Owner { get; set; } = null!;
    
    /// <summary>
    /// All campaigns within this world.
    /// </summary>
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    
    /// <summary>
    /// All articles scoped to this world (Wiki articles, Characters, etc.).
    /// </summary>
    public ICollection<Article> Articles { get; set; } = new List<Article>();

    /// <summary>
    /// External links associated with this world (Roll20, D&D Beyond, etc.).
    /// </summary>
    public ICollection<WorldLink> Links { get; set; } = new List<WorldLink>();

    /// <summary>
    /// Documents stored in blob storage associated with this world.
    /// </summary>
    public ICollection<WorldDocument> Documents { get; set; } = new List<WorldDocument>();

    /// <summary>
    /// All members of this world (GMs, Players, Observers).
    /// </summary>
    public ICollection<WorldMember> Members { get; set; } = new List<WorldMember>();

    /// <summary>
    /// All invitation codes for this world.
    /// </summary>
    public ICollection<WorldInvitation> Invitations { get; set; } = new List<WorldInvitation>();
}
