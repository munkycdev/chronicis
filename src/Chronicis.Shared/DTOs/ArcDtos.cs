namespace Chronicis.Shared.DTOs;

/// <summary>
/// Full Arc details for display and editing.
/// </summary>
public class ArcDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int SessionCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// Lightweight Arc DTO for tree view display.
/// </summary>
public class ArcTreeDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int SessionCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for creating new arcs.
/// </summary>
public class ArcCreateDto
{
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// Request DTO for updating existing arcs.
/// </summary>
public class ArcUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}
