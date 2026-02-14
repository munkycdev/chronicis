using Chronicis.Shared.Enums;

using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// DTO for creating a new quest.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuestCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QuestStatus? Status { get; set; }
    public bool? IsGmOnly { get; set; }
    public int? SortOrder { get; set; }
}
