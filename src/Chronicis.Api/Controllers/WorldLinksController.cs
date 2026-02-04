using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for World Link management (external resource links).
/// </summary>
[ApiController]
[Route("worlds/{worldId:guid}/links")]
[Authorize]
public class WorldLinksController : ControllerBase
{
    private readonly ChronicisDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldLinksController> _logger;

    public WorldLinksController(
        ChronicisDbContext db,
        ICurrentUserService currentUserService,
        ILogger<WorldLinksController> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /worlds/{worldId}/links - Get all links for a world.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorldLinkDto>>> GetWorldLinks(Guid worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting links for world {WorldId} by user {UserId}", worldId, user.Id);

        // Verify user has access to the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        var links = await _db.WorldLinks
            .AsNoTracking()
            .Where(wl => wl.WorldId == worldId)
            .OrderBy(wl => wl.Title)
            .Select(wl => new WorldLinkDto
            {
                Id = wl.Id,
                WorldId = wl.WorldId,
                Url = wl.Url,
                Title = wl.Title,
                Description = wl.Description,
                CreatedAt = wl.CreatedAt
            })
            .ToListAsync();

        return Ok(links);
    }

    /// <summary>
    /// POST /worlds/{worldId}/links - Create a new link for a world.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorldLinkDto>> CreateWorldLink(Guid worldId, [FromBody] WorldLinkCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user owns the world
        var world = await _db.Worlds
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Url) || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { error = "URL and Title are required" });
        }

        // Validate URL format
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new { error = "Invalid URL format. Must be a valid http or https URL." });
        }

        _logger.LogDebug("Creating link '{Title}' for world {WorldId} by user {UserId}",
            dto.Title, worldId, user.Id);

        var link = new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Url = dto.Url.Trim(),
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.WorldLinks.Add(link);
        await _db.SaveChangesAsync();

        var result = new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        };

        return CreatedAtAction(nameof(GetWorldLinks), new { worldId }, result);
    }

    /// <summary>
    /// PUT /worlds/{worldId}/links/{linkId} - Update an existing world link.
    /// </summary>
    [HttpPut("{linkId:guid}")]
    public async Task<ActionResult<WorldLinkDto>> UpdateWorldLink(
        Guid worldId,
        Guid linkId,
        [FromBody] WorldLinkUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user owns the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        var link = await _db.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            return NotFound(new { error = "Link not found" });
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Url) || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { error = "URL and Title are required" });
        }

        // Validate URL format
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new { error = "Invalid URL format. Must be a valid http or https URL." });
        }

        _logger.LogDebug("Updating link {LinkId} for world {WorldId} by user {UserId}",
            linkId, worldId, user.Id);

        link.Url = dto.Url.Trim();
        link.Title = dto.Title.Trim();
        link.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _db.SaveChangesAsync();

        var result = new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        };

        return Ok(result);
    }

    /// <summary>
    /// DELETE /worlds/{worldId}/links/{linkId} - Delete a world link.
    /// </summary>
    [HttpDelete("{linkId:guid}")]
    public async Task<IActionResult> DeleteWorldLink(Guid worldId, Guid linkId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        // Verify user owns the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            return NotFound(new { error = "World not found or access denied" });
        }

        var link = await _db.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            return NotFound(new { error = "Link not found" });
        }

        _logger.LogDebug("Deleting link {LinkId} for world {WorldId} by user {UserId}",
            linkId, worldId, user.Id);

        _db.WorldLinks.Remove(link);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
