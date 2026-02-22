using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronicis.Client.Components.Admin;

/// <summary>
/// Admin panel for listing and deleting worlds across the entire platform.
/// Requires the caller (Utilities page) to have already verified sysadmin status.
/// </summary>
public partial class AdminWorldsPanel : ComponentBase
{
    [Inject] private IAdminApiService AdminApi { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<AdminWorldsPanel> Logger { get; set; } = default!;

    private List<AdminWorldSummaryDto> _worlds = new();
    private bool _isLoading;
    private string? _loadError;

    private static readonly DialogOptions _dialogOptions = new()
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
    };

    protected override async Task OnInitializedAsync()
        => await LoadWorldsAsync();

    internal async Task LoadWorldsAsync()
    {
        _isLoading = true;
        _loadError = null;
        StateHasChanged();

        try
        {
            _worlds = await AdminApi.GetWorldSummariesAsync();
        }
        catch (Exception ex)
        {
            _loadError = "Failed to load worlds.";
            Logger.LogError(ex, "Error loading admin world summaries");
        }
        finally
        {
            _isLoading = false;
        }
    }

    internal async Task OpenDeleteDialogAsync(AdminWorldSummaryDto world)
    {
        var parameters = new DialogParameters<DeleteWorldDialog>
        {
            { x => x.WorldName, world.Name }
        };

        var dialog = await DialogService.ShowAsync<DeleteWorldDialog>(
            $"Delete \"{world.Name}\"", parameters, _dialogOptions);

        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await ExecuteDeleteAsync(world);
        }
    }

    private async Task ExecuteDeleteAsync(AdminWorldSummaryDto world)
    {
        try
        {
            var success = await AdminApi.DeleteWorldAsync(world.Id);
            if (success)
            {
                Snackbar.Add($"World \"{world.Name}\" deleted.", Severity.Success);
                await LoadWorldsAsync();
            }
            else
            {
                Snackbar.Add("Delete failed — world may have already been removed.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("An unexpected error occurred during deletion.", Severity.Error);
            Logger.LogError(ex, "Error deleting world {WorldId}", world.Id);
        }
    }
}
