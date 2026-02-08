using Chronicis.Shared.DTOs.Quests;
using MudBlazor;
using System.Net.Http.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for communicating with the Quest API.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// Handles RowVersion concurrency for quest updates.
/// </summary>
public class QuestApiService : IQuestApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<QuestApiService> _logger;
    private readonly ISnackbar _snackbar;

    public QuestApiService(
        HttpClient http, 
        ILogger<QuestApiService> logger,
        ISnackbar snackbar)
    {
        _http = http;
        _logger = logger;
        _snackbar = snackbar;
    }

    public async Task<List<QuestDto>> GetArcQuestsAsync(Guid arcId)
    {
        return await _http.GetListAsync<QuestDto>(
            $"arcs/{arcId}/quests",
            _logger,
            $"quests for arc {arcId}");
    }

    public async Task<QuestDto?> GetQuestAsync(Guid questId)
    {
        return await _http.GetEntityAsync<QuestDto>(
            $"quests/{questId}",
            _logger,
            $"quest {questId}");
    }

    public async Task<QuestDto?> CreateQuestAsync(Guid arcId, QuestCreateDto createDto)
    {
        return await _http.PostEntityAsync<QuestDto>(
            $"arcs/{arcId}/quests",
            createDto,
            _logger,
            "create quest");
    }

    public async Task<QuestDto?> UpdateQuestAsync(Guid questId, QuestEditDto editDto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"quests/{questId}", editDto);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // 409 Conflict - rowversion mismatch
                _logger.LogWarning("Quest {QuestId} update conflict (stale RowVersion)", questId);
                
                // Read the current server state from response body
                var currentQuest = await response.Content.ReadFromJsonAsync<QuestDto>();
                if (currentQuest != null)
                {
                    _snackbar.Add("Quest was modified by another user. Changes reloaded.", Severity.Warning);
                    return currentQuest;
                }
                
                _snackbar.Add("Quest update conflict. Please reload.", Severity.Error);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<QuestDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quest {QuestId}", questId);
            _snackbar.Add("Failed to update quest", Severity.Error);
            return null;
        }
    }

    public async Task<bool> DeleteQuestAsync(Guid questId)
    {
        return await _http.DeleteEntityAsync(
            $"quests/{questId}",
            _logger,
            $"quest {questId}");
    }

    public async Task<PagedResult<QuestUpdateEntryDto>> GetQuestUpdatesAsync(Guid questId, int skip = 0, int take = 20)
    {
        return await _http.GetEntityAsync<PagedResult<QuestUpdateEntryDto>>(
            $"quests/{questId}/updates?skip={skip}&take={take}",
            _logger,
            $"updates for quest {questId}") 
            ?? new PagedResult<QuestUpdateEntryDto>();
    }

    public async Task<QuestUpdateEntryDto?> AddQuestUpdateAsync(Guid questId, QuestUpdateCreateDto createDto)
    {
        return await _http.PostEntityAsync<QuestUpdateEntryDto>(
            $"quests/{questId}/updates",
            createDto,
            _logger,
            "add quest update");
    }

    public async Task<bool> DeleteQuestUpdateAsync(Guid questId, Guid updateId)
    {
        return await _http.DeleteEntityAsync(
            $"quests/{questId}/updates/{updateId}",
            _logger,
            $"quest update {updateId}");
    }
}
