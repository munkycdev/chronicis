using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for QuestUpdate management.
/// </summary>
[ApiController]
[Authorize]
public class QuestUpdatesController : ControllerBase
{
    private readonly IQuestUpdateService _questUpdateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<QuestUpdatesController> _logger;

    public QuestUpdatesController(
        IQuestUpdateService questUpdateService,
        ICurrentUserService currentUserService,
        ILogger<QuestUpdatesController> logger)
    {
        _questUpdateService = questUpdateService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /quests/{questId}/updates - Get paginated quest updates.
    /// </summary>
    [HttpGet("quests/{questId:guid}/updates")]
    public async Task<ActionResult<PagedResult<QuestUpdateEntryDto>>> GetQuestUpdates(
        Guid questId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting quest updates for quest {QuestId} (skip: {Skip}, take: {Take}) for user {UserId}",
            questId, skip, take, user.Id);

        var result = await _questUpdateService.GetQuestUpdatesAsync(questId, user.Id, skip, take);

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
    /// POST /quests/{questId}/updates - Create a new quest update (GM or Player).
    /// </summary>
    [HttpPost("quests/{questId:guid}/updates")]
    public async Task<ActionResult<QuestUpdateEntryDto>> CreateQuestUpdate(
        Guid questId,
        [FromBody] QuestUpdateCreateDto dto)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (dto == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogDebug("Creating quest update for quest {QuestId} by user {UserId}", questId, user.Id);

        var result = await _questUpdateService.CreateQuestUpdateAsync(questId, dto, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(
                nameof(GetQuestUpdates),
                new { questId = questId, skip = 0, take = 20 },
                result.Value),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }

    /// <summary>
    /// DELETE /quests/{questId}/updates/{updateId} - Delete a quest update.
    /// GM can delete any, Player can delete own only.
    /// </summary>
    [HttpDelete("quests/{questId:guid}/updates/{updateId:guid}")]
    public async Task<IActionResult> DeleteQuestUpdate(Guid questId, Guid updateId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Deleting quest update {UpdateId} from quest {QuestId} for user {UserId}",
            updateId, questId, user.Id);

        var result = await _questUpdateService.DeleteQuestUpdateAsync(questId, updateId, user.Id);

        return result.Status switch
        {
            ServiceStatus.Success => NoContent(),
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = "An unexpected error occurred" })
        };
    }
}
