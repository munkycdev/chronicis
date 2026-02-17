using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Basic campaign information for lists
/// </summary>
[ExcludeFromCodeCoverage]
public class CampaignDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public int ArcCount { get; set; }
}

/// <summary>
/// Detailed campaign information including arcs
/// </summary>
[ExcludeFromCodeCoverage]
public class CampaignDetailDto : CampaignDto
{
    public List<ArcDto> Arcs { get; set; } = new();
}

/// <summary>
/// DTO for creating a new campaign
/// </summary>
[ExcludeFromCodeCoverage]
public class CampaignCreateDto
{
    public Guid WorldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating a campaign
/// </summary>
[ExcludeFromCodeCoverage]
public class CampaignUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
