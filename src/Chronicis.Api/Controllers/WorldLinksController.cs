using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for World Link management (external resource links).
/// </summary>
[ApiController]
[Route("worlds/{worldId:guid}/links")]
[Authorize]
public class WorldLinksController : ControllerBase
{
    private readonly IWorldLinkService _worldLinkService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldLinksController> _logger;

    public WorldLinksController(
        IWorldLinkService worldLinkService,
        ICurrentUserService currentUserService,
        ILogger<WorldLinksController> logger)
    {
        _worldLinkService = worldLinkService;
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
        var result = await _worldLinkService.GetWorldLinksAsync(worldId, user.Id);
        return result.Status == ServiceStatus.NotFound
            ? NotFound(new { error = result.ErrorMessage })
            : Ok(result.Value);
    }

    /// <summary>
    /// POST /worlds/{worldId}/links - Create a new link for a world.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorldLinkDto>> CreateWorldLink(Guid worldId, [FromBody] WorldLinkCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

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

        _logger.LogDebugSanitized("Creating link '{Title}' for world {WorldId} by user {UserId}",
            dto.Title, worldId, user.Id);

        var result = await _worldLinkService.CreateWorldLinkAsync(worldId, dto, user.Id);
        return result.Status == ServiceStatus.NotFound
            ? NotFound(new { error = result.ErrorMessage })
            : CreatedAtAction(nameof(GetWorldLinks), new { worldId }, result.Value);
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

        _logger.LogDebug("Updating link {LinkId} for world {WorldId} by user {UserId}", linkId, worldId, user.Id);

        var result = await _worldLinkService.UpdateWorldLinkAsync(worldId, linkId, dto, user.Id);
        return result.Status == ServiceStatus.NotFound
            ? NotFound(new { error = result.ErrorMessage })
            : Ok(result.Value);
    }

    /// <summary>
    /// DELETE /worlds/{worldId}/links/{linkId} - Delete a world link.
    /// </summary>
    [HttpDelete("{linkId:guid}")]
    public async Task<IActionResult> DeleteWorldLink(Guid worldId, Guid linkId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        _logger.LogDebug("Deleting link {LinkId} for world {WorldId} by user {UserId}",
            linkId, worldId, user.Id);
        var result = await _worldLinkService.DeleteWorldLinkAsync(worldId, linkId, user.Id);
        return result.Status == ServiceStatus.NotFound
            ? NotFound(new { error = result.ErrorMessage })
            : NoContent();
    }
}
