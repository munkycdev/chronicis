using Chronicis.Shared.Enums;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents an invitation code that allows users to join a world.
/// Invitations can be single-use or multi-use with optional expiration.
/// </summary>
public class WorldInvitation
{
    /// <summary>
    /// Unique identifier for this invitation.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The world this invitation is for.
    /// </summary>
    public Guid WorldId { get; set; }
    
    /// <summary>
    /// The invitation code (e.g., "FROG-AXLE"). 
    /// 8 characters, uppercase, formatted as XXXX-XXXX.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The role that will be assigned to users who join via this invitation.
    /// Defaults to Player.
    /// </summary>
    public WorldRole Role { get; set; } = WorldRole.Player;
    
    /// <summary>
    /// User who created this invitation.
    /// </summary>
    public Guid CreatedBy { get; set; }
    
    /// <summary>
    /// When the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the invitation expires. Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Maximum number of times this invitation can be used. Null means unlimited.
    /// </summary>
    public int? MaxUses { get; set; }
    
    /// <summary>
    /// Number of times this invitation has been used.
    /// </summary>
    public int UsedCount { get; set; } = 0;
    
    /// <summary>
    /// Whether this invitation is active and can be used.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // ===== Navigation Properties =====
    
    /// <summary>
    /// The world this invitation is for.
    /// </summary>
    public World World { get; set; } = null!;
    
    /// <summary>
    /// The user who created this invitation.
    /// </summary>
    public User Creator { get; set; } = null!;
}
