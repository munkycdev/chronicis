using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// Single quest update entry in the timeline.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuestUpdateEntryDto
{
    public Guid Id { get; set; }
    public Guid QuestId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
    public string? SessionTitle { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? CreatedByAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
