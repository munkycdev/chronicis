using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for World management.
/// </summary>
[ApiController]
[Route("worlds")]
[Authorize]
public class WorldsController : ControllerBase
{
    private readonly IWorldService _worldService;
    private readonly IExportService _exportService;
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldsController> _logger;

    public WorldsController(
        IWorldService worldService,
        IExportService exportService,
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<WorldsController> logger)
    {
        _worldService = worldService;
        _exportService = exportService;
        _context = context;
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

    // ===== Link Suggestions =====

    /// <summary>
    /// GET /worlds/{id}/link-suggestions - Get link suggestions for autocomplete based on a search query.
    /// </summary>
    [HttpGet("{id:guid}/link-suggestions")]
    public async Task<ActionResult<LinkSuggestionsResponseDto>> GetLinkSuggestions(
        Guid id,
        [FromQuery] string query)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new LinkSuggestionsResponseDto());
        }

        _logger.LogInformation("Getting link suggestions for query '{Query}' in world {WorldId}", query, id);

        // Verify user has access to the world
        var hasAccess = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == id && wm.UserId == user.Id);

        if (!hasAccess)
        {
            return Forbid();
        }

        var normalizedQuery = query.ToLowerInvariant();

        // Search articles by title match
        var titleMatches = await _context.Articles
            .Where(a => a.WorldId == id)
            .Where(a => a.Title != null && a.Title.ToLower().Contains(normalizedQuery))
            .OrderBy(a => a.Title)
            .Take(20)
            .Select(a => new LinkSuggestionDto
            {
                ArticleId = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                ArticleType = a.Type,
                DisplayPath = "",
                MatchedAlias = null // Title match, no alias
            })
            .ToListAsync();

        // Search articles by alias match (excluding those already found by title)
        var titleMatchIds = titleMatches.Select(t => t.ArticleId).ToHashSet();
        
        var aliasMatches = await _context.ArticleAliases
            .Include(aa => aa.Article)
            .Where(aa => aa.Article.WorldId == id)
            .Where(aa => aa.AliasText.ToLower().Contains(normalizedQuery))
            .Where(aa => !titleMatchIds.Contains(aa.ArticleId))
            .OrderBy(aa => aa.AliasText)
            .Take(20)
            .Select(aa => new LinkSuggestionDto
            {
                ArticleId = aa.ArticleId,
                Title = aa.Article.Title ?? "Untitled",
                Slug = aa.Article.Slug,
                ArticleType = aa.Article.Type,
                DisplayPath = "",
                MatchedAlias = aa.AliasText // This matched via alias
            })
            .ToListAsync();

        // Combine results: title matches first, then alias matches
        var suggestions = titleMatches
            .Concat(aliasMatches)
            .Take(20)
            .ToList();

        // Build display paths for each suggestion
        foreach (var suggestion in suggestions)
        {
            suggestion.DisplayPath = await BuildDisplayPathAsync(suggestion.ArticleId);
        }

        return Ok(new LinkSuggestionsResponseDto { Suggestions = suggestions });
    }

    /// <summary>
    /// Builds a display path for an article (stripping the first level).
    /// </summary>
    private async Task<string> BuildDisplayPathAsync(Guid articleId)
    {
        var pathParts = new List<string>();
        var currentId = articleId;
        var visited = new HashSet<Guid>();

        // Walk up the tree
        while (currentId != Guid.Empty && !visited.Contains(currentId))
        {
            visited.Add(currentId);

            var article = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => new { a.Title, a.ParentId })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            pathParts.Insert(0, article.Title ?? "Untitled");
            currentId = article.ParentId ?? Guid.Empty;
        }

        // Strip the first level (world root) if there are multiple levels
        if (pathParts.Count > 1)
        {
            pathParts.RemoveAt(0);
        }

        return string.Join(" / ", pathParts);
    }
}
