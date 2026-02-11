using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for world invitation management
/// </summary>
public interface IWorldInvitationService
{
    /// <summary>
    /// Get all invitations for a world
    /// </summary>
    Task<List<WorldInvitationDto>> GetInvitationsAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Create a new invitation for a world
    /// </summary>
    Task<WorldInvitationDto?> CreateInvitationAsync(Guid worldId, WorldInvitationCreateDto dto, Guid userId);

    /// <summary>
    /// Revoke (deactivate) an invitation
    /// </summary>
    Task<bool> RevokeInvitationAsync(Guid worldId, Guid invitationId, Guid userId);

    /// <summary>
    /// Join a world using an invitation code
    /// </summary>
    Task<WorldJoinResultDto> JoinWorldAsync(string code, Guid userId);
}
