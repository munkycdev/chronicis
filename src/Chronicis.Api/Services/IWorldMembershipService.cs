using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for world membership and access control
/// </summary>
public interface IWorldMembershipService
{
    /// <summary>
    /// Check if user has access to a world (is a member)
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Check if user owns a world
    /// </summary>
    Task<bool> UserOwnsWorldAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Get all members of a world
    /// </summary>
    Task<List<WorldMemberDto>> GetMembersAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Update a member's role in a world
    /// </summary>
    Task<WorldMemberDto?> UpdateMemberRoleAsync(Guid worldId, Guid memberId, WorldMemberUpdateDto dto, Guid userId);

    /// <summary>
    /// Remove a member from a world
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid worldId, Guid memberId, Guid userId);
}
