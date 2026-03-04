using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for WorldMap management (create, list, get, basemap upload).
/// </summary>
[Route("world/{worldId:guid}/maps")]
[Authorize]
public class MapsController : ControllerBase
{
    private readonly IWorldMapService _worldMapService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MapsController> _logger;

    public MapsController(
        IWorldMapService worldMapService,
        ICurrentUserService currentUserService,
        ILogger<MapsController> logger)
    {
        _worldMapService = worldMapService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// POST /world/{worldId}/maps — Create a new map with default hidden layers.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MapDto>> CreateMap(Guid worldId, [FromBody] MapCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogTraceSanitized("User {UserId} creating map in world {WorldId}", user.Id, worldId);

        try
        {
            var result = await _worldMapService.CreateMapAsync(worldId, user.Id, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized map creation");
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /world/{worldId}/maps/{mapId} — Get full map metadata.
    /// </summary>
    [HttpGet("{mapId:guid}")]
    public async Task<ActionResult<MapDto>> GetMap(Guid worldId, Guid mapId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} getting map {MapId}", user.Id, mapId);

        var map = await _worldMapService.GetMapAsync(mapId, user.Id);

        if (map == null)
        {
            return NotFound(new { error = "Map not found or access denied" });
        }

        return Ok(map);
    }

    /// <summary>
    /// PUT /world/{worldId}/maps/{mapId} — Update map metadata.
    /// </summary>
    [HttpPut("{mapId:guid}")]
    public async Task<ActionResult<MapDto>> UpdateMap(Guid worldId, Guid mapId, [FromBody] MapUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogTraceSanitized("User {UserId} updating map {MapId}", user.Id, mapId);

        try
        {
            var updated = await _worldMapService.UpdateMapAsync(worldId, mapId, user.Id, dto);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized map update");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Map update target not found");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /world/{worldId}/maps — List all maps for a world, sorted by name.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MapSummaryDto>>> ListMapsForWorld(Guid worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} listing maps for world {WorldId}", user.Id, worldId);

        try
        {
            var maps = await _worldMapService.ListMapsForWorldAsync(worldId, user.Id);
            return Ok(maps);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized map list");
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /world/{worldId}/maps/{mapId} — Permanently delete map metadata and all map blobs.
    /// </summary>
    [HttpDelete("{mapId:guid}")]
    public async Task<IActionResult> DeleteMap(Guid worldId, Guid mapId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} deleting map {MapId}", user.Id, mapId);

        try
        {
            await _worldMapService.DeleteMapAsync(worldId, mapId, user.Id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized map delete");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Map delete target not found");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /world/{worldId}/maps/{mapId}/request-basemap-upload — Generate a SAS URL for uploading a basemap.
    /// </summary>
    [HttpPost("{mapId:guid}/request-basemap-upload")]
    public async Task<ActionResult<RequestBasemapUploadResponseDto>> RequestBasemapUpload(
        Guid worldId, Guid mapId, [FromBody] RequestBasemapUploadDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Invalid request body" });
        }

        _logger.LogTraceSanitized("User {UserId} requesting basemap upload for map {MapId}", user.Id, mapId);

        try
        {
            var result = await _worldMapService.RequestBasemapUploadAsync(worldId, mapId, user.Id, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized basemap upload request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarningSanitized(ex, "Invalid basemap upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Map not found for basemap upload");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /world/{worldId}/maps/{mapId}/confirm-basemap-upload — Confirm the basemap upload completed.
    /// Filename and content-type were already persisted during the SAS request; no body required.
    /// </summary>
    [HttpPost("{mapId:guid}/confirm-basemap-upload")]
    public async Task<ActionResult<MapDto>> ConfirmBasemapUpload(Guid worldId, Guid mapId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} confirming basemap upload for map {MapId}", user.Id, mapId);

        try
        {
            var result = await _worldMapService.ConfirmBasemapUploadAsync(worldId, mapId, user.Id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized basemap confirm");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Map not found for basemap confirm");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /world/{worldId}/maps/{mapId}/basemap — Get short-lived SAS read URL for basemap.
    /// </summary>
    [HttpGet("{mapId:guid}/basemap")]
    public async Task<ActionResult<GetBasemapReadUrlResponseDto>> GetBasemapReadUrl(Guid worldId, Guid mapId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} requesting basemap read URL for map {MapId}", user.Id, mapId);

        try
        {
            var result = await _worldMapService.GetBasemapReadUrlAsync(worldId, mapId, user.Id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized basemap read request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Basemap read request not found");
            return NotFound(new { error = ex.Message });
        }
    }
}
