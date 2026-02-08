namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// DTO for creating a new quest update.
/// </summary>
public class QuestUpdateCreateDto
{
    public string Body { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
}
