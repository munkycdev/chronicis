using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for World API operations
/// </summary>
public interface IWorldApiService
{
    /// <summary>
    /// Get all worlds the user has access to
    /// </summary>
    Task<List<WorldDto>> GetWorldsAsync();

    /// <summary>
    /// Get a specific world with its campaigns
    /// </summary>
    Task<WorldDetailDto?> GetWorldAsync(Guid worldId);

    /// <summary>
    /// Create a new world
    /// </summary>
    Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto);

    /// <summary>
    /// Update a world
    /// </summary>
    Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto);

    // ===== World Links =====

    /// <summary>
    /// Get all links for a world (sorted alphabetically)
    /// </summary>
    Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId);

    /// <summary>
    /// Create a new link for a world
    /// </summary>
    Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto);

    /// <summary>
    /// Update an existing world link
    /// </summary>
    Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto);

    /// <summary>
    /// Delete a world link
    /// </summary>
    Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId);

    // ===== World Documents =====

    /// <summary>
    /// Request a document upload (get SAS URL for client upload)
    /// </summary>
    Task<WorldDocumentUploadResponseDto?> RequestDocumentUploadAsync(Guid worldId, WorldDocumentUploadRequestDto dto);

    /// <summary>
    /// Confirm a document upload completed successfully
    /// </summary>
    Task<WorldDocumentDto?> ConfirmDocumentUploadAsync(Guid worldId, Guid documentId);

    /// <summary>
    /// Get all documents for a world
    /// </summary>
    Task<List<WorldDocumentDto>> GetWorldDocumentsAsync(Guid worldId);

    /// <summary>
    /// Download a document's content from the API.
    /// </summary>
    Task<DocumentDownloadResult?> DownloadDocumentAsync(Guid documentId);

    /// <summary>
    /// Update document metadata (title, description)
    /// </summary>
    Task<WorldDocumentDto?> UpdateDocumentAsync(Guid worldId, Guid documentId, WorldDocumentUpdateDto dto);

    /// <summary>
    /// Delete a document
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid worldId, Guid documentId);

    // ===== Public Sharing =====

    /// <summary>
    /// Check if a public slug is available for a world
    /// </summary>
    Task<PublicSlugCheckResultDto?> CheckPublicSlugAsync(Guid worldId, string slug);

    // ===== Member Management =====

    /// <summary>
    /// Get all members of a world
    /// </summary>
    Task<List<WorldMemberDto>> GetMembersAsync(Guid worldId);

    /// <summary>
    /// Update a member's role
    /// </summary>
    Task<WorldMemberDto?> UpdateMemberRoleAsync(Guid worldId, Guid memberId, WorldMemberUpdateDto dto);

    /// <summary>
    /// Remove a member from a world
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid worldId, Guid memberId);

    // ===== Invitation Management =====

    /// <summary>
    /// Get all invitations for a world
    /// </summary>
    Task<List<WorldInvitationDto>> GetInvitationsAsync(Guid worldId);

    /// <summary>
    /// Create a new invitation
    /// </summary>
    Task<WorldInvitationDto?> CreateInvitationAsync(Guid worldId, WorldInvitationCreateDto dto);

    /// <summary>
    /// Revoke an invitation
    /// </summary>
    Task<bool> RevokeInvitationAsync(Guid worldId, Guid invitationId);

    /// <summary>
    /// Join a world using an invitation code
    /// </summary>
    Task<WorldJoinResultDto?> JoinWorldAsync(string code);
}
