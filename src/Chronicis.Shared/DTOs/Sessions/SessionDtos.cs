using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs.Sessions;

/// <summary>
/// Session entity response DTO.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionDto
{
    public Guid Id { get; set; }
    public Guid ArcId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? SessionDate { get; set; }
    public string? PublicNotes { get; set; }
    public string? PrivateNotes { get; set; }
    public string? AiSummary { get; set; }
    public DateTime? AiSummaryGeneratedAt { get; set; }
    public Guid? AiSummaryGeneratedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Request DTO for creating a Session entity.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionCreateDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime? SessionDate { get; set; }
}

/// <summary>
/// Request DTO for updating Session notes (GM only).
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionUpdateDto
{
    public string? PublicNotes { get; set; }
    public string? PrivateNotes { get; set; }
}
