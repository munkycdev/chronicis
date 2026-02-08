namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a timeline entry for a quest. QuestUpdates are append-only
/// and provide a chronological log of quest progression during sessions.
/// </summary>
public class QuestUpdate
{
    /// <summary>
    /// Unique identifier for the quest update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The quest this update belongs to.
    /// </summary>
    public Guid QuestId { get; set; }

    /// <summary>
    /// Optional reference to the session where this update occurred.
    /// Must be an Article with Type=Session and same ArcId as the quest.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Update content (HTML from TipTap editor). Required, non-empty.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// User who created this update.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// When the update was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====

    /// <summary>
    /// The quest this update belongs to.
    /// </summary>
    public Quest Quest { get; set; } = null!;

    /// <summary>
    /// The session article where this update occurred (if any).
    /// </summary>
    public Article? Session { get; set; }

    /// <summary>
    /// User who created this update.
    /// </summary>
    public User Creator { get; set; } = null!;
}
