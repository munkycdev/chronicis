using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// World member information
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public WorldRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public Guid? InvitedBy { get; set; }
    public string? InviterName { get; set; }
}

/// <summary>
/// DTO for updating a member's role in a world
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldMemberUpdateDto
{
    public WorldRole Role { get; set; }
}

/// <summary>
/// World invitation information
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldInvitationDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string Code { get; set; } = string.Empty;
    public WorldRole Role { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new world invitation
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldInvitationCreateDto
{
    public WorldRole Role { get; set; } = WorldRole.Player;
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
}

/// <summary>
/// DTO for joining a world via invitation code
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldJoinDto
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Result of attempting to join a world
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldJoinResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? WorldId { get; set; }
    public string? WorldName { get; set; }
    public WorldRole? AssignedRole { get; set; }
}
