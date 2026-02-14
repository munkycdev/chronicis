using Chronicis.Shared.Enums;

using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs.Quests;

/// <summary>
/// DTO for editing an existing quest.
/// Includes RowVersion for optimistic concurrency.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuestEditDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public QuestStatus? Status { get; set; }
    public bool? IsGmOnly { get; set; }
    public int? SortOrder { get; set; }

    /// <summary>
    /// Base64-encoded rowversion from the last read.
    /// Required for concurrency checking.
    /// </summary>
    public string RowVersion { get; set; } = string.Empty;
}
