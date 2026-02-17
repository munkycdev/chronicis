using Bunit;
using MudBlazor.Services;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Base test context that includes MudBlazor service registration.
/// Use this for testing components that depend on MudBlazor components.
/// </summary>
public class MudBlazorTestContext : TestContext
{
    public MudBlazorTestContext()
    {
        // Register MudBlazor services required for MudBlazor components
        Services.AddMudServices();

        // Setup JSInterop to handle MudBlazor's JavaScript interop calls
        // This prevents errors when MudBlazor components try to call JavaScript
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow any JS calls without explicit setup
    }
}
