using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for World API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class WorldApiService : IWorldApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<WorldApiService> _logger;

    public WorldApiService(HttpClient http, ILogger<WorldApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<WorldDto>> GetWorldsAsync()
    {
        return await _http.GetListAsync<WorldDto>(
            "worlds",
            _logger,
            "worlds");
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId)
    {
        return await _http.GetEntityAsync<WorldDetailDto>(
            $"worlds/{worldId}",
            _logger,
            $"world {worldId}");
    }

    public async Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldDto>(
            "worlds",
            dto,
            _logger,
            "world");
    }

    public async Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldDto>(
            $"worlds/{worldId}",
            dto,
            _logger,
            $"world {worldId}");
    }

    // ===== World Links =====

    public async Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldLinkDto>(
            $"worlds/{worldId}/links",
            _logger,
            $"links for world {worldId}");
    }

    public async Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldLinkDto>(
            $"worlds/{worldId}/links",
            dto,
            _logger,
            $"link for world {worldId}");
    }

    public async Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldLinkDto>(
            $"worlds/{worldId}/links/{linkId}",
            dto,
            _logger,
            $"link {linkId} for world {worldId}");
    }

    public async Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId)
    {
        return await _http.DeleteEntityAsync(
            $"worlds/{worldId}/links/{linkId}",
            _logger,
            $"link {linkId} from world {worldId}");
    }

    // ===== Public Sharing =====

    public async Task<PublicSlugCheckResultDto?> CheckPublicSlugAsync(Guid worldId, string slug)
    {
        var dto = new PublicSlugCheckDto { Slug = slug };
        return await _http.PostEntityAsync<PublicSlugCheckResultDto>(
            $"worlds/{worldId}/check-public-slug",
            dto,
            _logger,
            $"public slug check for world {worldId}");
    }

    // ===== Member Management =====

    public async Task<List<WorldMemberDto>> GetMembersAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldMemberDto>(
            $"worlds/{worldId}/members",
            _logger,
            $"members for world {worldId}");
    }

    public async Task<WorldMemberDto?> UpdateMemberRoleAsync(Guid worldId, Guid memberId, WorldMemberUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldMemberDto>(
            $"worlds/{worldId}/members/{memberId}",
            dto,
            _logger,
            $"member {memberId} in world {worldId}");
    }

    public async Task<bool> RemoveMemberAsync(Guid worldId, Guid memberId)
    {
        return await _http.DeleteEntityAsync(
            $"worlds/{worldId}/members/{memberId}",
            _logger,
            $"member {memberId} from world {worldId}");
    }

    // ===== Invitation Management =====

    public async Task<List<WorldInvitationDto>> GetInvitationsAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldInvitationDto>(
            $"worlds/{worldId}/invitations",
            _logger,
            $"invitations for world {worldId}");
    }

    public async Task<WorldInvitationDto?> CreateInvitationAsync(Guid worldId, WorldInvitationCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldInvitationDto>(
            $"worlds/{worldId}/invitations",
            dto,
            _logger,
            $"invitation for world {worldId}");
    }

    public async Task<bool> RevokeInvitationAsync(Guid worldId, Guid invitationId)
    {
        return await _http.DeleteEntityAsync(
            $"worlds/{worldId}/invitations/{invitationId}",
            _logger,
            $"invitation {invitationId} from world {worldId}");
    }

    public async Task<WorldJoinResultDto?> JoinWorldAsync(string code)
    {
        var dto = new WorldJoinDto { Code = code };
        return await _http.PostEntityAsync<WorldJoinResultDto>(
            "worlds/join",
            dto,
            _logger,
            "join world");
    }

    // ===== World Documents =====

    public async Task<WorldDocumentUploadResponseDto?> RequestDocumentUploadAsync(
        Guid worldId,
        WorldDocumentUploadRequestDto dto)
    {
        return await _http.PostEntityAsync<WorldDocumentUploadResponseDto>(
            $"worlds/{worldId}/documents/request-upload",
            dto,
            _logger,
            $"document upload request for world {worldId}");
    }

    public async Task<WorldDocumentDto?> ConfirmDocumentUploadAsync(Guid worldId, Guid documentId)
    {
        var dto = new WorldDocumentConfirmUploadDto { DocumentId = documentId };
        return await _http.PostEntityAsync<WorldDocumentDto>(
            $"worlds/{worldId}/documents/{documentId}/confirm",
            dto,
            _logger,
            $"document upload confirmation for {documentId}");
    }

    public async Task<List<WorldDocumentDto>> GetWorldDocumentsAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldDocumentDto>(
            $"worlds/{worldId}/documents",
            _logger,
            $"documents for world {worldId}");
    }

    public async Task<DocumentDownloadResult?> DownloadDocumentAsync(Guid documentId)
    {
        var downloadInfo = await _http.GetEntityAsync<WorldDocumentDownloadDto>(
            $"/documents/{documentId}/content",
            _logger,
            $"download URL for document {documentId}");

        if (downloadInfo == null)
            return null;

        return new DocumentDownloadResult(
            downloadInfo.DownloadUrl,
            downloadInfo.FileName,
            downloadInfo.ContentType,
            downloadInfo.FileSizeBytes);
    }

    public async Task<WorldDocumentDto?> UpdateDocumentAsync(
        Guid worldId,
        Guid documentId,
        WorldDocumentUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldDocumentDto>(
            $"worlds/{worldId}/documents/{documentId}",
            dto,
            _logger,
            $"document {documentId} for world {worldId}");
    }

    public async Task<bool> DeleteDocumentAsync(Guid worldId, Guid documentId)
    {
        return await _http.DeleteEntityAsync(
            $"worlds/{worldId}/documents/{documentId}",
            _logger,
            $"document {documentId} from world {worldId}");
    }
}
