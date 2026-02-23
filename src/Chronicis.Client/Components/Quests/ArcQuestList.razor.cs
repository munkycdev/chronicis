using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronicis.Client.Components.Quests;

public partial class ArcQuestList : ComponentBase
{
    [Parameter, EditorRequired]
    public Guid ArcId { get; set; }

    [Parameter]
    public bool IsGm { get; set; }

    [Parameter]
    public EventCallback<QuestDto> OnEditQuest { get; set; }

    [Inject] private IQuestApiService QuestApi { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ArcQuestList> Logger { get; set; } = default!;

    private List<QuestDto> _quests = new();
    private bool _isLoading;
    private bool _isCreating;
    private bool _isDeleting;
    private bool _isGm;
    private string? _loadingError;

    protected override async Task OnInitializedAsync()
    {
        _isGm = IsGm;
        await LoadQuestsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _isGm = IsGm;

        // Reload if ArcId changes
        if (ArcId != Guid.Empty)
        {
            await LoadQuestsAsync();
        }
    }

    private async Task LoadQuestsAsync()
    {
        _isLoading = true;
        _loadingError = null;
        StateHasChanged();

        try
        {
            _quests = await QuestApi.GetArcQuestsAsync(ArcId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load quests for arc {ArcId}", ArcId);
            _loadingError = ex.Message;
            _quests = new();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CreateQuest()
    {
        if (!_isGm || _isCreating)
            return;

        _isCreating = true;
        StateHasChanged();

        try
        {
            var parameters = new DialogParameters
            {
                { "ArcId", ArcId }
            };

            var dialog = await DialogService.ShowAsync<CreateQuestDialog>("Create Quest", parameters);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                await LoadQuestsAsync();
                Snackbar.Add("Quest created successfully", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create quest");
            Snackbar.Add($"Failed to create quest: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isCreating = false;
            StateHasChanged();
        }
    }

    private async Task EditQuest(QuestDto quest)
    {
        if (!_isGm)
            return;

        await OnEditQuest.InvokeAsync(quest);
    }

    private async Task DeleteQuest(QuestDto quest)
    {
        if (!_isGm || _isDeleting)
            return;

        var confirmed = await DialogService.ShowMessageBox(
            "Delete Quest",
            $"Are you sure you want to delete '{quest.Title}'? This will also delete all quest updates. This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed != true)
            return;

        _isDeleting = true;
        StateHasChanged();

        try
        {
            var success = await QuestApi.DeleteQuestAsync(quest.Id);
            if (success)
            {
                Snackbar.Add("Quest deleted successfully", Severity.Success);
                await LoadQuestsAsync();
            }
            else
            {
                Snackbar.Add("Failed to delete quest", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete quest {QuestId}", quest.Id);
            Snackbar.Add($"Error deleting quest: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }
}
