using Chronicis.Shared.Enums;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a user's membership in a campaign with their assigned role.
/// This is the many-to-many relationship between Users and Campaigns.
/// </summary>
public class CampaignMember
{
    /// <summary>
    /// Unique identifier for this membership record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The campaign this membership is for.
    /// </summary>
    public Guid CampaignId { get; set; }
    
    /// <summary>
    /// The user who is a member.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The user's role in this campaign (DM, Player, Observer).
    /// </summary>
    public CampaignRole Role { get; set; }
    
    /// <summary>
    /// When the user joined this campaign.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional character name for Players.
    /// Max 100 characters.
    /// </summary>
    public string? CharacterName { get; set; }
    
    // ===== Navigation Properties =====
    
    /// <summary>
    /// The campaign this membership is for.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;
    
    /// <summary>
    /// The user who is a member.
    /// </summary>
    public User User { get; set; } = null!;
}
