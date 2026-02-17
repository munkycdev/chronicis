using MudBlazor;
using MudBlazor.Services;

namespace Chronicis.Client.Extensions;

/// <summary>
/// Extension methods for configuring MudBlazor services.
/// </summary>
public static class MudBlazorServiceExtensions
{
    /// <summary>
    /// Adds MudBlazor services with Chronicis-specific configuration.
    /// </summary>
    public static IServiceCollection AddChronicisMudBlazor(this IServiceCollection services)
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 4000;
            config.SnackbarConfiguration.HideTransitionDuration = 300;
            config.SnackbarConfiguration.ShowTransitionDuration = 300;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        return services;
    }
}
