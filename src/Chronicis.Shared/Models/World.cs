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
}
