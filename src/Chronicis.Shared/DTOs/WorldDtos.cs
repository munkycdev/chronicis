using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Basic world information for lists
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CampaignCount { get; set; }
    public int MemberCount { get; set; }

    /// <summary>
    /// The ID of the WorldRoot article (top-level article for this world)
    /// </summary>
    public Guid? WorldRootArticleId { get; set; }

    /// <summary>
    /// Whether this world is publicly accessible to anonymous users.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether this world is a tutorial/onboarding world.
    /// </summary>
    public bool IsTutorial { get; set; }

}

/// <summary>
/// Detailed world information including campaigns and members
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldDetailDto : WorldDto
{
    public string? PrivateNotes { get; set; }
    public List<CampaignDto> Campaigns { get; set; } = new();
    public List<WorldMemberDto> Members { get; set; } = new();
    public List<WorldInvitationDto> Invitations { get; set; } = new();
}

/// <summary>
/// DTO for creating a new world
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating a world
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrivateNotes { get; set; }

    /// <summary>
    /// Whether to make the world publicly accessible.
    /// Null means no change to current value.
    /// </summary>
    public bool? IsPublic { get; set; }
}
