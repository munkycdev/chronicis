using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Dashboard data.
/// </summary>
[ApiController]
[Route("dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardReadService _dashboardReadService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardReadService dashboardReadService,
        ICurrentUserService currentUserService,
        ILogger<DashboardController> logger)
    {
        _dashboardReadService = dashboardReadService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/dashboard - Get aggregated dashboard data for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting dashboard for user {UserId}", user.Id);
        var dashboard = await _dashboardReadService.GetDashboardAsync(user.Id, user.DisplayName);
        return Ok(dashboard);
    }
}
