using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Arc management.
/// </summary>
[ApiController]
[Route("arcs")]
[Authorize]
public class ArcsController : ControllerBase
{
    private readonly IArcService _arcService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ArcsController> _logger;

    public ArcsController(
        IArcService arcService,
        ICurrentUserService currentUserService,
        ILogger<ArcsController> logger)
    {
        _arcService = arcService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/arcs/{id} - Get a specific arc.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArcDto>> GetArc(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting arc {ArcId} for user {UserId}", id, user.Id);

        var arc = await _arcService.GetArcAsync(id, user.Id);

        if (arc == null)
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        return Ok(arc);
    }

    /// <summary>
    /// POST /api/arcs - Create a new arc.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ArcDto>> CreateArc([FromBody] ArcCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        if (dto.CampaignId == Guid.Empty)
        {
            return BadRequest(new { error = "CampaignId is required" });
        }

        _logger.LogDebug("Creating arc '{Name}' in campaign {CampaignId} for user {UserId}",
            dto.Name, dto.CampaignId, user.Id);

        var arc = await _arcService.CreateArcAsync(dto, user.Id);

        if (arc == null)
        {
            return StatusCode(403, new { error = "Access denied or failed to create arc" });
        }

        return CreatedAtAction(nameof(GetArc), new { id = arc.Id }, arc);
    }

    /// <summary>
    /// PUT /api/arcs/{id} - Update an arc.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArcDto>> UpdateArc(Guid id, [FromBody] ArcUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogDebug("Updating arc {ArcId} for user {UserId}", id, user.Id);

        var arc = await _arcService.UpdateArcAsync(id, dto, user.Id);

        if (arc == null)
        {
            return NotFound(new { error = "Arc not found or access denied" });
        }

        return Ok(arc);
    }

    /// <summary>
    /// DELETE /api/arcs/{id} - Delete an arc (only if empty).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteArc(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Deleting arc {ArcId} for user {UserId}", id, user.Id);

        var success = await _arcService.DeleteArcAsync(id, user.Id);

        if (!success)
        {
            return BadRequest(new { error = "Arc not found, access denied, or arc is not empty" });
        }

        return NoContent();
    }

    /// <summary>
    /// POST /api/arcs/{id}/activate - Activate an arc for quick session creation.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateArc(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Activating arc {ArcId} for user {UserId}", id, user.Id);

        var success = await _arcService.ActivateArcAsync(id, user.Id);

        if (!success)
        {
            return BadRequest(new { error = "Unable to activate arc. Arc not found or you don't have permission." });
        }

        return NoContent();
    }
}

/// <summary>
/// Campaign-scoped arc endpoints.
/// </summary>
[ApiController]
[Route("campaigns/{campaignId:guid}/arcs")]
[Authorize]
public class CampaignArcsController : ControllerBase
{
    private readonly IArcService _arcService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CampaignArcsController> _logger;

    public CampaignArcsController(
        IArcService arcService,
        ICurrentUserService currentUserService,
        ILogger<CampaignArcsController> logger)
    {
        _arcService = arcService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/campaigns/{campaignId}/arcs - Get all arcs for a campaign.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ArcDto>>> GetArcsByCampaign(Guid campaignId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting arcs for campaign {CampaignId} for user {UserId}", campaignId, user.Id);

        var arcs = await _arcService.GetArcsByCampaignAsync(campaignId, user.Id);
        return Ok(arcs);
    }
}
