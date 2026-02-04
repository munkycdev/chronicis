using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Summary Template operations.
/// </summary>
[ApiController]
[Route("summary")]
[Authorize]
public class SummaryController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SummaryController> _logger;

    public SummaryController(
        ISummaryService summaryService,
        ICurrentUserService currentUserService,
        ILogger<SummaryController> logger)
    {
        _summaryService = summaryService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/summary/templates - Get all available summary templates.
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<List<SummaryTemplateDto>>> GetTemplates()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting summary templates for user {UserId}", user.Id);

        var templates = await _summaryService.GetTemplatesAsync();
        return Ok(templates);
    }
}
