using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Base test context that includes MudBlazor service registration.
/// Use this for testing components that depend on MudBlazor components.
/// </summary>
[ExcludeFromCodeCoverage]
public class MudBlazorTestContext : TestContext
{
    public MudBlazorTestContext()
    {
        // Register MudBlazor services required for MudBlazor components
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IAppContextService>());

        // Allow rendering components that use constructor injection.
        ComponentFactories.Add(
            componentType => typeof(IComponent).IsAssignableFrom(componentType)
                && componentType.Namespace?.StartsWith("Chronicis.Client", StringComparison.Ordinal) == true,
            componentType => (IComponent)ActivatorUtilities.CreateInstance(Services, componentType));

        // Setup JSInterop to handle MudBlazor's JavaScript interop calls
        // This prevents errors when MudBlazor components try to call JavaScript
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow any JS calls without explicit setup
    }
}
