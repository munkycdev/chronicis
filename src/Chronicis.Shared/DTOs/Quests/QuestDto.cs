using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// Complete quest data transfer object.
/// </summary>
public class QuestDto
{
    public Guid Id { get; set; }
    public Guid ArcId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QuestStatus Status { get; set; }
    public bool IsGmOnly { get; set; }
    public int SortOrder { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Base64-encoded rowversion for optimistic concurrency.
    /// </summary>
    public string RowVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Total number of updates for this quest.
    /// </summary>
    public int UpdateCount { get; set; }
}
