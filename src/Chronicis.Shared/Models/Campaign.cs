namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a Campaign - a gaming group's collaborative space within a World.
/// Multiple users collaborate within a Campaign with role-based access.
/// Sequential campaigns within a World share resources (Wiki, Characters).
/// </summary>
public class Campaign
{
    /// <summary>
    /// Unique identifier for the campaign.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The world this campaign belongs to.
    /// </summary>
    public Guid WorldId { get; set; }
    
    /// <summary>
    /// Display name of the campaign (e.g., "Dragon Heist", "Curse of Strahd").
    /// Max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the campaign.
    /// Max 1000 characters.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The user who owns this campaign (the DM).
    /// </summary>
    public Guid OwnerId { get; set; }
    
    /// <summary>
    /// When the campaign was created in Chronicis.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the campaign actually started (first session IRL).
    /// Null if not yet started.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the campaign ended.
    /// Null if still ongoing.
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    // ===== Navigation Properties =====
    
    /// <summary>
    /// The world this campaign belongs to.
    /// </summary>
    public World World { get; set; } = null!;
    
    /// <summary>
    /// The user who owns this campaign.
    /// </summary>
    public User Owner { get; set; } = null!;
    
    /// <summary>
    /// All members of this campaign (DM, Players, Observers).
    /// </summary>
    public ICollection<CampaignMember> Members { get; set; } = new List<CampaignMember>();
    
    /// <summary>
    /// All articles scoped to this campaign (Sessions, Acts, etc.).
    /// </summary>
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
