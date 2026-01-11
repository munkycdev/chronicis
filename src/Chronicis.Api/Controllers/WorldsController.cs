using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for World management.
/// </summary>
[ApiController]
[Route("api/worlds")]
[Authorize]
public class WorldsController : ControllerBase
{
    private readonly IWorldService _worldService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldsController> _logger;

    public WorldsController(
        IWorldService worldService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        ILogger<WorldsController> logger)
    {
        _worldService = worldService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/worlds - Get all worlds the user has access to.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorldDto>>> GetWorlds()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting worlds for user {UserId}", user.Id);

        var worlds = await _worldService.GetUserWorldsAsync(user.Id);
        return Ok(worlds);
    }

    /// <summary>
    /// GET /api/worlds/{id} - Get a specific world with its campaigns.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorldDto>> GetWorld(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting world {WorldId} for user {UserId}", id, user.Id);

        var world = await _worldService.GetWorldAsync(id, user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        return Ok(world);
    }

    /// <summary>
    /// POST /api/worlds - Create a new world.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorldDto>> CreateWorld([FromBody] WorldCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogInformation("Creating world '{Name}' for user {UserId}", dto.Name, user.Id);

        var world = await _worldService.CreateWorldAsync(dto, user.Id);

        return CreatedAtAction(nameof(GetWorld), new { id = world.Id }, world);
    }

    /// <summary>
    /// PUT /api/worlds/{id} - Update a world.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorldDto>> UpdateWorld(Guid id, [FromBody] WorldUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogInformation("Updating world {WorldId} for user {UserId}", id, user.Id);

        var world = await _worldService.UpdateWorldAsync(id, dto, user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        return Ok(world);
    }

    /// <summary>
    /// POST /api/worlds/{id}/check-public-slug - Check if a public slug is available.
    /// </summary>
    [HttpPost("{id:guid}/check-public-slug")]
    public async Task<ActionResult<PublicSlugCheckResultDto>> CheckPublicSlug(Guid id, [FromBody] PublicSlugCheckDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user owns this world
        var world = await _worldService.GetWorldAsync(id, user.Id);
        if (world == null || world.OwnerId != user.Id)
        {
            return StatusCode(403, new { error = "Only the world owner can check public slugs" });
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Slug))
        {
            return BadRequest(new { error = "Slug is required" });
        }

        _logger.LogInformation("Checking public slug '{Slug}' for world {WorldId}", dto.Slug, id);

        var result = await _worldService.CheckPublicSlugAsync(dto.Slug, id);
        return Ok(result);
    }

    // ===== Member Management =====

    /// <summary>
    /// GET /api/worlds/{id}/members - Get all members of a world.
    /// </summary>
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IEnumerable<WorldMemberDto>>> GetWorldMembers(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting members for world {WorldId}", id);

        var members = await _worldService.GetMembersAsync(id, user.Id);
        return Ok(members);
    }

    /// <summary>
    /// PUT /api/worlds/{worldId}/members/{memberId} - Update a member's role.
    /// </summary>
    [HttpPut("{worldId:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<WorldMemberDto>> UpdateWorldMember(
        Guid worldId,
        Guid memberId,
        [FromBody] WorldMemberUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Invalid request body" });
        }

        _logger.LogInformation("Updating member {MemberId} in world {WorldId}", memberId, worldId);

        var member = await _worldService.UpdateMemberRoleAsync(worldId, memberId, dto, user.Id);

        if (member == null)
        {
            return NotFound(new { error = "Member not found, access denied, or cannot demote last GM" });
        }

        return Ok(member);
    }

    /// <summary>
    /// DELETE /api/worlds/{worldId}/members/{memberId} - Remove a member from a world.
    /// </summary>
    [HttpDelete("{worldId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveWorldMember(Guid worldId, Guid memberId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Removing member {MemberId} from world {WorldId}", memberId, worldId);

        var success = await _worldService.RemoveMemberAsync(worldId, memberId, user.Id);

        if (!success)
        {
            return NotFound(new { error = "Member not found, access denied, or cannot remove last GM" });
        }

        return NoContent();
    }

    // ===== Invitation Management =====

    /// <summary>
    /// GET /api/worlds/{id}/invitations - Get all invitations for a world.
    /// </summary>
    [HttpGet("{id:guid}/invitations")]
    public async Task<ActionResult<IEnumerable<WorldInvitationDto>>> GetWorldInvitations(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting invitations for world {WorldId}", id);

        var invitations = await _worldService.GetInvitationsAsync(id, user.Id);
        return Ok(invitations);
    }

    /// <summary>
    /// POST /api/worlds/{id}/invitations - Create a new invitation.
    /// </summary>
    [HttpPost("{id:guid}/invitations")]
    public async Task<ActionResult<WorldInvitationDto>> CreateWorldInvitation(
        Guid id,
        [FromBody] WorldInvitationCreateDto? dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        dto ??= new WorldInvitationCreateDto(); // Use defaults if body is empty

        _logger.LogInformation("Creating invitation for world {WorldId}", id);

        var invitation = await _worldService.CreateInvitationAsync(id, dto, user.Id);

        if (invitation == null)
        {
            return StatusCode(403, new { error = "Access denied or failed to create invitation" });
        }

        return CreatedAtAction(nameof(GetWorldInvitations), new { id = id }, invitation);
    }

    /// <summary>
    /// DELETE /api/worlds/{worldId}/invitations/{invitationId} - Revoke an invitation.
    /// </summary>
    [HttpDelete("{worldId:guid}/invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeWorldInvitation(Guid worldId, Guid invitationId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Revoking invitation {InvitationId} for world {WorldId}", invitationId, worldId);

        var success = await _worldService.RevokeInvitationAsync(worldId, invitationId, user.Id);

        if (!success)
        {
            return NotFound(new { error = "Invitation not found or access denied" });
        }

        return NoContent();
    }

    /// <summary>
    /// POST /api/worlds/join - Join a world using an invitation code.
    /// </summary>
    [HttpPost("join")]
    public async Task<ActionResult<WorldJoinResultDto>> JoinWorld([FromBody] WorldJoinDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Code))
        {
            return BadRequest(new { error = "Invitation code is required" });
        }

        _logger.LogInformation("User {UserId} attempting to join world with code {Code}", user.Id, dto.Code);

        var result = await _worldService.JoinWorldAsync(dto.Code, user.Id);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    // ===== Export =====

    /// <summary>
    /// GET /api/worlds/{id}/export - Export world to a markdown zip archive.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportWorld(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Exporting world {WorldId} for user {UserId}", id, user.Id);

        var zipData = await _exportService.ExportWorldToMarkdownAsync(id, user.Id);

        if (zipData == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        // Get world name for filename
        var world = await _worldService.GetWorldAsync(id, user.Id);
        var worldName = world?.Name ?? "world";
        var safeWorldName = string.Join("_", worldName.Split(Path.GetInvalidFileNameChars()));
        if (safeWorldName.Length > 50) safeWorldName = safeWorldName[..50];
        var fileName = $"{safeWorldName}_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

        return File(zipData, "application/zip", fileName);
    }
}
