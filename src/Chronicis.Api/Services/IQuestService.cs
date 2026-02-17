using Chronicis.Api.Models;
using Chronicis.Shared.DTOs.Quests;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for Quest operations.
/// </summary>
public interface IQuestService
{
    /// <summary>
    /// Get all quests for an arc. Non-GM users will not see IsGmOnly quests.
    /// </summary>
    Task<ServiceResult<List<QuestDto>>> GetQuestsByArcAsync(Guid arcId, Guid userId);

    /// <summary>
    /// Get a single quest by ID with update count.
    /// </summary>
    Task<ServiceResult<QuestDto>> GetQuestAsync(Guid questId, Guid userId);

    /// <summary>
    /// Create a new quest (GM only).
    /// </summary>
    Task<ServiceResult<QuestDto>> CreateQuestAsync(Guid arcId, QuestCreateDto dto, Guid userId);

    /// <summary>
    /// Update an existing quest (GM only). Includes RowVersion concurrency check.
    /// </summary>
    Task<ServiceResult<QuestDto>> UpdateQuestAsync(Guid questId, QuestEditDto dto, Guid userId);

    /// <summary>
    /// Delete a quest and all its updates (GM only).
    /// </summary>
    Task<ServiceResult<bool>> DeleteQuestAsync(Guid questId, Guid userId);
}
