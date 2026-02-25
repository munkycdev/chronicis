using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for resolving contextual tutorial content.
/// </summary>
[ApiController]
[Route("tutorials")]
[Authorize]
public class TutorialsController : ControllerBase
{
    private readonly ITutorialService _tutorialService;
    private readonly ILogger<TutorialsController> _logger;

    public TutorialsController(ITutorialService tutorialService, ILogger<TutorialsController> logger)
    {
        _tutorialService = tutorialService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/tutorials/resolve?pageType={pageType} - Resolves the tutorial article for the current page context.
    /// </summary>
    [HttpGet("resolve")]
    public async Task<ActionResult<TutorialDto>> Resolve([FromQuery] string pageType)
    {
        try
        {
            var tutorial = await _tutorialService.ResolveAsync(pageType);
            if (tutorial == null)
            {
                return NotFound(new { error = "Tutorial not found" });
            }

            return Ok(tutorial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tutorial for page type {PageType}", pageType);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}
