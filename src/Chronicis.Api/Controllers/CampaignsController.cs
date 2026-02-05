using Chronicis.Shared.Extensions;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Campaign management.
/// </summary>
[ApiController]
[Route("campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ICampaignService campaignService,
        ICurrentUserService currentUserService,
        ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/campaigns/{id} - Get a specific campaign.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> GetCampaign(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting campaign {CampaignId} for user {UserId}", id, user.Id);

        var campaign = await _campaignService.GetCampaignAsync(id, user.Id);

        if (campaign == null)
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        return Ok(campaign);
    }

    /// <summary>
    /// POST /api/campaigns - Create a new campaign.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> CreateCampaign([FromBody] CampaignCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        if (dto.WorldId == Guid.Empty)
        {
            return BadRequest(new { error = "WorldId is required" });
        }

        _logger.LogDebugSanitized("Creating campaign '{Name}' in world {WorldId} for user {UserId}",
            dto.Name, dto.WorldId, user.Id);

        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(dto, user.Id);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/campaigns/{id} - Update a campaign.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> UpdateCampaign(Guid id, [FromBody] CampaignUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        _logger.LogDebug("Updating campaign {CampaignId} for user {UserId}", id, user.Id);

        var campaign = await _campaignService.UpdateCampaignAsync(id, dto, user.Id);

        if (campaign == null)
        {
            return NotFound(new { error = "Campaign not found or access denied" });
        }

        return Ok(campaign);
    }

    /// <summary>
    /// POST /api/campaigns/{id}/activate - Activate a campaign.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateCampaign(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        _logger.LogDebug("Activating campaign {CampaignId} for user {UserId}", id, user.Id);

        var success = await _campaignService.ActivateCampaignAsync(id, user.Id);

        if (!success)
        {
            return BadRequest(new { error = "Unable to activate campaign. Campaign not found or you don't have permission." });
        }

        return NoContent();
    }
}

/// <summary>
/// Active context endpoints - nested under worlds but related to campaigns.
/// </summary>
[ApiController]
[Route("worlds/{worldId:guid}")]
[Authorize]
public class WorldActiveContextController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldActiveContextController> _logger;

    public WorldActiveContextController(
        ICampaignService campaignService,
        ICurrentUserService currentUserService,
        ILogger<WorldActiveContextController> logger)
    {
        _campaignService = campaignService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/worlds/{worldId}/active-context - Get the active context for a world.
    /// </summary>
    [HttpGet("active-context")]
    public async Task<ActionResult<ActiveContextDto>> GetActiveContext(Guid worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        _logger.LogDebug("Getting active context for world {WorldId} for user {UserId}", worldId, user.Id);

        var activeContext = await _campaignService.GetActiveContextAsync(worldId, user.Id);
        return Ok(activeContext);
    }
}
