using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints restricted to system administrators.
/// Authorization is enforced inside <see cref="IAdminService"/>; the controller
/// maps <see cref="UnauthorizedAccessException"/> to 403 Forbidden.
/// </summary>
[ApiController]
[Route("admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// GET /admin/worlds — returns a summary of every world in the system.
    /// </summary>
    [HttpGet("worlds")]
    public async Task<ActionResult<List<AdminWorldSummaryDto>>> GetWorlds()
    {
        try
        {
            var summaries = await _adminService.GetAllWorldSummariesAsync();
            return Ok(summaries);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to access admin world listing");
            return Forbid();
        }
    }

    /// <summary>
    /// DELETE /admin/worlds/{id} — permanently deletes a world and all its data.
    /// </summary>
    [HttpDelete("worlds/{id:guid}")]
    public async Task<IActionResult> DeleteWorld(Guid id)
    {
        try
        {
            var deleted = await _adminService.DeleteWorldAsync(id);
            return deleted ? NoContent() : NotFound(new { error = "World not found" });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to delete world {WorldId}", id);
            return Forbid();
        }
    }
}
