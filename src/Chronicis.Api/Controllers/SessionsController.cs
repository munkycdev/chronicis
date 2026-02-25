using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Session entity creation and updates.
/// </summary>
[ApiController]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionService sessionService,
        ICurrentUserService currentUserService,
        ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/arcs/{arcId}/sessions - List Session entities for an arc (tree/navigation).
    /// </summary>
    [HttpGet("arcs/{arcId:guid}/sessions")]
    public async Task<ActionResult<List<SessionTreeDto>>> GetSessionsByArc(Guid arcId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        var result = await _sessionService.GetSessionsByArcAsync(arcId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value ?? new List<SessionTreeDto>()),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// GET /api/sessions/{sessionId} - Get a Session entity by id.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<SessionDto>> GetSession(Guid sessionId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        var result = await _sessionService.GetSessionAsync(sessionId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// POST /api/arcs/{arcId}/sessions - Create a session and one default public SessionNote.
    /// </summary>
    [HttpPost("arcs/{arcId:guid}/sessions")]
    public async Task<ActionResult<SessionDto>> CreateSession(Guid arcId, [FromBody] SessionCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogDebugSanitized("Creating session '{Name}' in arc {ArcId} for user {UserId}",
            dto.Name, arcId, user.Id);

        var result = await _sessionService.CreateSessionAsync(arcId, dto, user.Id, user.DisplayName);

        return result.Status switch
        {
            ServiceStatus.Success => Created($"sessions/{result.Value!.Id}", result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// PATCH /api/sessions/{sessionId} - Update editable Session fields (GM only).
    /// </summary>
    [HttpPatch("sessions/{sessionId:guid}")]
    public async Task<ActionResult<SessionDto>> UpdateSessionNotes(Guid sessionId, [FromBody] SessionUpdateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogDebug("Updating session {SessionId} by user {UserId}", sessionId, user.Id);

        var result = await _sessionService.UpdateSessionNotesAsync(sessionId, dto, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// POST /api/sessions/{sessionId}/ai-summary/generate - Generate a public-safe AI summary for a session.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/ai-summary/generate")]
    public async Task<ActionResult<SummaryGenerationDto>> GenerateAiSummary(Guid sessionId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        _logger.LogDebug("Generating AI summary for session {SessionId} by user {UserId}", sessionId, user.Id);

        var result = await _sessionService.GenerateAiSummaryAsync(sessionId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }
}
