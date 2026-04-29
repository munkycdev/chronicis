using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Full Arc details for display and editing.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArcDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrivateNotes { get; set; }
    public int SortOrder { get; set; }
    public int SessionCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CampaignSlug { get; set; } = string.Empty;
    public string WorldSlug { get; set; } = string.Empty;
}

/// <summary>
/// Lightweight Arc DTO for tree view display.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArcTreeDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int SessionCount { get; set; }
    public bool IsActive { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CampaignSlug { get; set; } = string.Empty;
    public string WorldSlug { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for creating new arcs.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArcCreateDto
{
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public string? Slug { get; set; }
}

/// <summary>
/// Request DTO for updating existing arcs.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArcUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrivateNotes { get; set; }
    public int? SortOrder { get; set; }
}
