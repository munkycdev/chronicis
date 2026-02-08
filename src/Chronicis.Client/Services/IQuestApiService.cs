using Chronicis.Shared.DTOs.Quests;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for communicating with the Quest API.
/// Provides intention-revealing methods for quest operations.
/// </summary>
public interface IQuestApiService
{
    /// <summary>
    /// Get all quests for an arc, filtered by user permissions.
    /// </summary>
    Task<List<QuestDto>> GetArcQuestsAsync(Guid arcId);

    /// <summary>
    /// Get a single quest with its recent updates.
    /// </summary>
    Task<QuestDto?> GetQuestAsync(Guid questId);

    /// <summary>
    /// Create a new quest in an arc.
    /// GM only.
    /// </summary>
    Task<QuestDto?> CreateQuestAsync(Guid arcId, QuestCreateDto createDto);

    /// <summary>
    /// Update an existing quest.
    /// Handles RowVersion concurrency.
    /// GM only.
    /// </summary>
    /// <returns>Updated quest, or null if 409 conflict occurred.</returns>
    Task<QuestDto?> UpdateQuestAsync(Guid questId, QuestEditDto editDto);

    /// <summary>
    /// Delete a quest and all its updates.
    /// GM only.
    /// </summary>
    Task<bool> DeleteQuestAsync(Guid questId);

    /// <summary>
    /// Get paginated quest updates for a quest.
    /// </summary>
    Task<PagedResult<QuestUpdateEntryDto>> GetQuestUpdatesAsync(Guid questId, int skip = 0, int take = 20);

    /// <summary>
    /// Add a new update to a quest.
    /// GM or Player.
    /// </summary>
    Task<QuestUpdateEntryDto?> AddQuestUpdateAsync(Guid questId, QuestUpdateCreateDto createDto);

    /// <summary>
    /// Delete a quest update.
    /// GM can delete any, Players can delete own only.
    /// </summary>
    Task<bool> DeleteQuestUpdateAsync(Guid questId, Guid updateId);
}
