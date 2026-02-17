using Chronicis.Client.Components.Dialogs;
using Chronicis.Shared.DTOs;
using MudBlazor;

namespace Chronicis.Client.Services;

/// <summary>
/// Implementation of IWorldCampaignFacade.
/// Wraps IAppContextService, IDialogService, and ISnackbar into a single facade.
/// </summary>
public class WorldCampaignFacade : IWorldCampaignFacade, IDisposable
{
    private readonly IAppContextService _appContext;
    private readonly IDialogService _dialogService;
    private readonly ISnackbar _snackbar;

    public WorldCampaignFacade(
        IAppContextService appContext,
        IDialogService dialogService,
        ISnackbar snackbar)
    {
        _appContext = appContext;
        _dialogService = dialogService;
        _snackbar = snackbar;

        // Forward the event
        _appContext.OnContextChanged += RaiseContextChanged;
    }

    public bool IsInitialized => _appContext.IsInitialized;
    public IReadOnlyList<WorldDto> Worlds => _appContext.Worlds;
    public Guid? CurrentWorldId => _appContext.CurrentWorldId;
    public Guid? CurrentCampaignId => _appContext.CurrentCampaignId;
    public WorldDetailDto? CurrentWorld => _appContext.CurrentWorld;

    public event Action? OnContextChanged;

    private void RaiseContextChanged()
    {
        OnContextChanged?.Invoke();
    }

    public async Task SelectWorldAsync(Guid worldId)
    {
        await _appContext.SelectWorldAsync(worldId);
    }

    public async Task SelectCampaignAsync(Guid? campaignId)
    {
        await _appContext.SelectCampaignAsync(campaignId);
    }

    public async Task<WorldDto?> CreateWorldAsync()
    {
        var dialog = await _dialogService.ShowAsync<CreateWorldDialog>("Create New World");
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is WorldDto newWorld)
        {
            await _appContext.RefreshWorldsAsync();
            await _appContext.SelectWorldAsync(newWorld.Id);
            _snackbar.Add($"World '{newWorld.Name}' created!", Severity.Success);
            return newWorld;
        }

        return null;
    }

    public async Task<CampaignDto?> CreateCampaignAsync(Guid worldId)
    {
        var parameters = new DialogParameters
        {
            ["WorldId"] = worldId
        };

        var dialog = await _dialogService.ShowAsync<CreateCampaignDialog>("Create New Campaign", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is CampaignDto newCampaign)
        {
            await _appContext.RefreshCurrentWorldAsync();
            await _appContext.SelectCampaignAsync(newCampaign.Id);
            _snackbar.Add($"Campaign '{newCampaign.Name}' created!", Severity.Success);
            return newCampaign;
        }

        return null;
    }

    public void Dispose()
    {
        _appContext.OnContextChanged -= RaiseContextChanged;
    }
}
