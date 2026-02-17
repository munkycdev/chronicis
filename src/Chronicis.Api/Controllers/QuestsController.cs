using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Quest management.
/// </summary>
[ApiController]
[Authorize]
public class QuestsController : ControllerBase
{
    private readonly IQuestService _questService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<QuestsController> _logger;

    public QuestsController(
        IQuestService questService,
        ICurrentUserService currentUserService,
        ILogger<QuestsController> logger)
    {
        _questService = questService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /arcs/{arcId}/quests - Get all quests for an arc.
    /// </summary>
    [HttpGet("arcs/{arcId:guid}/quests")]
    public async Task<ActionResult<List<QuestDto>>> GetQuestsByArc(Guid arcId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting quests for arc {ArcId} for user {UserId}", arcId, user.Id);

        var result = await _questService.GetQuestsByArcAsync(arcId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// POST /arcs/{arcId}/quests - Create a new quest (GM only).
    /// </summary>
    [HttpPost("arcs/{arcId:guid}/quests")]
    public async Task<ActionResult<QuestDto>> CreateQuest(Guid arcId, [FromBody] QuestCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogDebugSanitized("Creating quest '{Title}' in arc {ArcId} for user {UserId}",
            dto.Title, arcId, user.Id);

        var result = await _questService.CreateQuestAsync(arcId, dto, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(
                nameof(GetQuest),
                new { questId = result.Value!.Id },
                result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// GET /quests/{questId} - Get a specific quest with update count.
    /// </summary>
    [HttpGet("quests/{questId:guid}")]
    public async Task<ActionResult<QuestDto>> GetQuest(Guid questId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting quest {QuestId} for user {UserId}", questId, user.Id);

        var result = await _questService.GetQuestAsync(questId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// PUT /quests/{questId} - Update a quest (GM only). Includes RowVersion concurrency check.
    /// </summary>
    [HttpPut("quests/{questId:guid}")]
    public async Task<ActionResult<QuestDto>> UpdateQuest(Guid questId, [FromBody] QuestEditDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogDebug("Updating quest {QuestId} for user {UserId}", questId, user.Id);

        var result = await _questService.UpdateQuestAsync(questId, dto, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.Conflict => Conflict(new
            {
                error = result.ErrorMessage,
                currentState = result.Value // Include current QuestDto with latest RowVersion
            }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// DELETE /quests/{questId} - Delete a quest and all its updates (GM only).
    /// </summary>
    [HttpDelete("quests/{questId:guid}")]
    public async Task<IActionResult> DeleteQuest(Guid questId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Deleting quest {QuestId} for user {UserId}", questId, user.Id);

        var result = await _questService.DeleteQuestAsync(questId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => NoContent(),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }
}
