using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

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
            "api/worlds",
            _logger,
            "worlds");
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId)
    {
        return await _http.GetEntityAsync<WorldDetailDto>(
            $"api/worlds/{worldId}",
            _logger,
            $"world {worldId}");
    }

    public async Task<WorldDto?> CreateWorldAsync(WorldCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldDto>(
            "api/worlds",
            dto,
            _logger,
            "world");
    }

    public async Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldDto>(
            $"api/worlds/{worldId}",
            dto,
            _logger,
            $"world {worldId}");
    }

    // ===== World Links =====

    public async Task<List<WorldLinkDto>> GetWorldLinksAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links",
            _logger,
            $"links for world {worldId}");
    }

    public async Task<WorldLinkDto?> CreateWorldLinkAsync(Guid worldId, WorldLinkCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links",
            dto,
            _logger,
            $"link for world {worldId}");
    }

    public async Task<WorldLinkDto?> UpdateWorldLinkAsync(Guid worldId, Guid linkId, WorldLinkUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldLinkDto>(
            $"api/worlds/{worldId}/links/{linkId}",
            dto,
            _logger,
            $"link {linkId} for world {worldId}");
    }

    public async Task<bool> DeleteWorldLinkAsync(Guid worldId, Guid linkId)
    {
        return await _http.DeleteEntityAsync(
            $"api/worlds/{worldId}/links/{linkId}",
            _logger,
            $"link {linkId} from world {worldId}");
    }

    // ===== Public Sharing =====

    public async Task<PublicSlugCheckResultDto?> CheckPublicSlugAsync(Guid worldId, string slug)
    {
        var dto = new PublicSlugCheckDto { Slug = slug };
        return await _http.PostEntityAsync<PublicSlugCheckResultDto>(
            $"api/worlds/{worldId}/check-public-slug",
            dto,
            _logger,
            $"public slug check for world {worldId}");
    }

    // ===== Member Management =====

    public async Task<List<WorldMemberDto>> GetMembersAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldMemberDto>(
            $"api/worlds/{worldId}/members",
            _logger,
            $"members for world {worldId}");
    }

    public async Task<WorldMemberDto?> UpdateMemberRoleAsync(Guid worldId, Guid memberId, WorldMemberUpdateDto dto)
    {
        return await _http.PutEntityAsync<WorldMemberDto>(
            $"api/worlds/{worldId}/members/{memberId}",
            dto,
            _logger,
            $"member {memberId} in world {worldId}");
    }

    public async Task<bool> RemoveMemberAsync(Guid worldId, Guid memberId)
    {
        return await _http.DeleteEntityAsync(
            $"api/worlds/{worldId}/members/{memberId}",
            _logger,
            $"member {memberId} from world {worldId}");
    }

    // ===== Invitation Management =====

    public async Task<List<WorldInvitationDto>> GetInvitationsAsync(Guid worldId)
    {
        return await _http.GetListAsync<WorldInvitationDto>(
            $"api/worlds/{worldId}/invitations",
            _logger,
            $"invitations for world {worldId}");
    }

    public async Task<WorldInvitationDto?> CreateInvitationAsync(Guid worldId, WorldInvitationCreateDto dto)
    {
        return await _http.PostEntityAsync<WorldInvitationDto>(
            $"api/worlds/{worldId}/invitations",
            dto,
            _logger,
            $"invitation for world {worldId}");
    }

    public async Task<bool> RevokeInvitationAsync(Guid worldId, Guid invitationId)
    {
        return await _http.DeleteEntityAsync(
            $"api/worlds/{worldId}/invitations/{invitationId}",
            _logger,
            $"invitation {invitationId} from world {worldId}");
    }

    public async Task<WorldJoinResultDto?> JoinWorldAsync(string code)
    {
        var dto = new WorldJoinDto { Code = code };
        return await _http.PostEntityAsync<WorldJoinResultDto>(
            "api/worlds/join",
            dto,
            _logger,
            "join world");
    }
}
