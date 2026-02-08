using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronicis.Client.Components.Quests;

public partial class ArcQuestTimeline : ComponentBase
{
    [Parameter]
    public QuestDto? Quest { get; set; }

    [Parameter]
    public bool IsGm { get; set; }

    [Parameter]
    public Guid CurrentUserId { get; set; }

    [Inject] private IQuestApiService QuestApi { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private QuestDto? _quest;
    private List<QuestUpdateEntryDto> _updates = new();
    private bool _isLoading;
    private bool _isLoadingMore;
    private bool _hasMore;
    private int _skip = 0;
    private const int _pageSize = 20;

    protected override async Task OnParametersSetAsync()
    {
        // Quest changed - reset and reload
        if (_quest?.Id != Quest?.Id)
        {
            _quest = Quest;
            _updates.Clear();
            _skip = 0;
            _hasMore = false;

            if (_quest != null)
            {
                await LoadUpdatesAsync();
            }
        }
    }

    private async Task LoadUpdatesAsync()
    {
        if (_quest == null) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            var result = await QuestApi.GetQuestUpdatesAsync(_quest.Id, _skip, _pageSize);
            
            _updates.AddRange(result.Items);
            _hasMore = result.TotalCount > _updates.Count;
            _skip = _updates.Count;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load quest updates: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_quest == null || _isLoadingMore) return;

        _isLoadingMore = true;
        StateHasChanged();

        try
        {
            var result = await QuestApi.GetQuestUpdatesAsync(_quest.Id, _skip, _pageSize);
            
            _updates.AddRange(result.Items);
            _hasMore = result.TotalCount > _updates.Count;
            _skip = _updates.Count;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load more updates: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoadingMore = false;
            StateHasChanged();
        }
    }

    private bool CanDeleteUpdate(QuestUpdateEntryDto update)
    {
        // GM can delete any update, Player can delete own updates only
        return IsGm || update.CreatedBy == CurrentUserId;
    }

    private async Task DeleteUpdate(QuestUpdateEntryDto update)
    {
        if (_quest == null) return;

        var confirmed = await DialogService.ShowMessageBox(
            "Delete Update",
            "Are you sure you want to delete this quest update?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed == true)
        {
            var success = await QuestApi.DeleteQuestUpdateAsync(_quest.Id, update.Id);
            if (success)
            {
                _updates.Remove(update);
                Snackbar.Add("Quest update deleted", Severity.Success);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("Failed to delete quest update", Severity.Error);
            }
        }
    }
}
