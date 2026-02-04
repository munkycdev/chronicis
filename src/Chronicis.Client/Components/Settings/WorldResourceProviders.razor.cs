using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronicis.Client.Components.Settings;

public partial class WorldResourceProviders : ComponentBase
{
    [Inject] private ResourceProviderApiService ResourceProviderService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<WorldResourceProviders> Logger { get; set; } = default!;

    [Parameter] public Guid WorldId { get; set; }

    private List<WorldResourceProviderDto>? _providers;
    private bool _loading = true;
    private bool _updating = false;
    private bool _error = false;

    protected override async Task OnParametersSetAsync()
    {
        await LoadProviders();
    }

    private async Task LoadProviders()
    {
        _loading = true;
        _error = false;
        StateHasChanged();

        try
        {
            _providers = await ResourceProviderService.GetWorldProvidersAsync(WorldId);
            
            if (_providers == null)
            {
                _error = true;
                Logger.LogWarning("Failed to load providers for world {WorldId}", WorldId);
            }
            else
            {
                // Sort providers alphabetically by name
                _providers = _providers.OrderBy(p => p.Provider.Name).ToList();
            }
        }
        catch (Exception ex)
        {
            _error = true;
            Logger.LogError(ex, "Error loading providers for world {WorldId}", WorldId);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task OnToggleProvider(string providerCode, bool enabled)
    {
        _updating = true;
        StateHasChanged();

        try
        {
            var success = await ResourceProviderService.ToggleProviderAsync(WorldId, providerCode, enabled);

            if (success)
            {
                // Update local state
                var provider = _providers?.FirstOrDefault(p => p.Provider.Code == providerCode);
                if (provider != null)
                {
                    provider.IsEnabled = enabled;
                }

                Snackbar.Add(
                    $"{providerCode} {(enabled ? "enabled" : "disabled")} successfully",
                    Severity.Success);

                Logger.LogInformation(
                    "Provider {ProviderCode} {Action} for world {WorldId}",
                    providerCode,
                    enabled ? "enabled" : "disabled",
                    WorldId);
            }
            else
            {
                Snackbar.Add(
                    $"Failed to {(enabled ? "enable" : "disable")} {providerCode}",
                    Severity.Error);

                Logger.LogWarning(
                    "Failed to toggle provider {ProviderCode} for world {WorldId}",
                    providerCode,
                    WorldId);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                $"Error updating provider: {ex.Message}",
                Severity.Error);

            Logger.LogError(
                ex,
                "Error toggling provider {ProviderCode} for world {WorldId}",
                providerCode,
                WorldId);
        }
        finally
        {
            _updating = false;
            StateHasChanged();
        }
    }
}
