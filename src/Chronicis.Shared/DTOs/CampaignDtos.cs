using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Basic campaign information for lists
/// </summary>
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
    public int MemberCount { get; set; }
    public int ArcCount { get; set; }
}

/// <summary>
/// Detailed campaign information including members
/// </summary>
public class CampaignDetailDto : CampaignDto
{
    public List<CampaignMemberDto> Members { get; set; } = new();
    public List<ArcDto> Arcs { get; set; } = new();
}

/// <summary>
/// DTO for creating a new campaign
/// </summary>
public class CampaignCreateDto
{
    public Guid WorldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating a campaign
/// </summary>
public class CampaignUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

/// <summary>
/// Campaign member information
/// </summary>
public class CampaignMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public CampaignRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? CharacterName { get; set; }
}

/// <summary>
/// DTO for adding a member to a campaign
/// </summary>
public class CampaignMemberAddDto
{
    public string Email { get; set; } = string.Empty;
    public CampaignRole Role { get; set; } = CampaignRole.Player;
    public string? CharacterName { get; set; }
}

/// <summary>
/// DTO for updating a member's role
/// </summary>
public class CampaignMemberUpdateDto
{
    public CampaignRole Role { get; set; }
    public string? CharacterName { get; set; }
}
