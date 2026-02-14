using Chronicis.Shared.Enums;

using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a quest within an Arc. Quests track objectives and their progression
/// through status changes and timeline updates.
/// </summary>
[ExcludeFromCodeCoverage]
public class Quest
{
    /// <summary>
    /// Unique identifier for the quest.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The arc this quest belongs to.
    /// </summary>
    public Guid ArcId { get; set; }

    /// <summary>
    /// Quest title/name. Max 300 characters.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Quest description (HTML from TipTap editor). Nullable.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the quest.
    /// </summary>
    public QuestStatus Status { get; set; } = QuestStatus.Active;

    /// <summary>
    /// Whether this quest is visible only to GMs.
    /// Hidden quests (faction objectives, secret betrayals) not shown to players.
    /// </summary>
    public bool IsGmOnly { get; set; }

    /// <summary>
    /// Sort order for displaying quests within an arc.
    /// Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// User who created this quest.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// When the quest was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the quest was last updated. Updated on quest edit AND on QuestUpdate append.
    /// This enables "recent activity" sorting without querying QuestUpdates.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// SQL Server rowversion for optimistic concurrency control.
    /// EF Core uses this to detect concurrent modifications.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;

    // ===== Navigation Properties =====

    /// <summary>
    /// The arc this quest belongs to.
    /// </summary>
    public Arc Arc { get; set; } = null!;

    /// <summary>
    /// User who created this quest.
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// Timeline of updates for this quest.
    /// </summary>
    public ICollection<QuestUpdate> Updates { get; set; } = new List<QuestUpdate>();
}
