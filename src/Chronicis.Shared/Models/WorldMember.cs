using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a user's membership in a world with their assigned role.
/// World membership grants access to all campaigns within that world.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMember
{
    /// <summary>
    /// Unique identifier for this membership record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The world this membership is for.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// The user who is a member.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's role in this world (GM, Player, Observer).
    /// </summary>
    public WorldRole Role { get; set; }

    /// <summary>
    /// When the user joined this world.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who invited this member. Null if they are the world creator or joined via public access.
    /// </summary>
    public Guid? InvitedBy { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The world this membership is for.
    /// </summary>
    public World World { get; set; } = null!;

    /// <summary>
    /// The user who is a member.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The user who invited this member.
    /// </summary>
    public User? Inviter { get; set; }
}
