using Chronicis.Api.Models;
using Chronicis.Shared.DTOs.Quests;

namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for QuestUpdate operations.
/// </summary>
public interface IQuestUpdateService
{
    /// <summary>
    /// Get paginated quest updates for a quest.
    /// </summary>
    Task<ServiceResult<PagedResult<QuestUpdateEntryDto>>> GetQuestUpdatesAsync(
        Guid questId, 
        Guid userId, 
        int skip = 0, 
        int take = 20);

    /// <summary>
    /// Create a new quest update (GM or Player, not Observer).
    /// Also updates the parent Quest's UpdatedAt timestamp.
    /// </summary>
    Task<ServiceResult<QuestUpdateEntryDto>> CreateQuestUpdateAsync(
        Guid questId, 
        QuestUpdateCreateDto dto, 
        Guid userId);

    /// <summary>
    /// Delete a quest update. GM can delete any, Player can delete own only.
    /// </summary>
    Task<ServiceResult<bool>> DeleteQuestUpdateAsync(
        Guid questId, 
        Guid updateId, 
        Guid userId);
}
