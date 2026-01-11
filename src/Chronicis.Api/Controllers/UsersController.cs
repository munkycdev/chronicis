using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for User profile management.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users/me - Get the current user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUserProfile()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting profile for user {UserId}", user.Id);

        var profile = await _userService.GetUserProfileAsync(user.Id);

        if (profile == null)
        {
            return NotFound(new { error = "User profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// POST /api/users/me/complete-onboarding - Mark onboarding as complete.
    /// </summary>
    [HttpPost("me/complete-onboarding")]
    public async Task<IActionResult> CompleteOnboarding()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Completing onboarding for user {UserId}", user.Id);

        var success = await _userService.CompleteOnboardingAsync(user.Id);

        if (!success)
        {
            return BadRequest(new { error = "Failed to complete onboarding" });
        }

        return NoContent();
    }
}
