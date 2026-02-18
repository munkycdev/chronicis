using Chronicis.Client.Extensions;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class ApplicationServiceExtensionsTests
{
    [Fact]
    public void AddChronicisApplicationServices_RegistersCoreApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("ChronicisApi", c => c.BaseAddress = new Uri("https://api.example/"));
        services.AddHttpClient("ChronicisPublicApi", c => c.BaseAddress = new Uri("https://public.example/"));
        services.AddSingleton(Substitute.For<ISnackbar>());
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddSingleton<NavigationManager>(new TestNavigationManager("https://client.example/"));

        var returned = services.AddChronicisApplicationServices();

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IArticleApiService>());
        Assert.NotNull(provider.GetRequiredService<IQuestApiService>());
        Assert.NotNull(provider.GetRequiredService<IExportApiService>());
        Assert.NotNull(provider.GetRequiredService<IPublicApiService>());
        Assert.NotNull(provider.GetRequiredService<IHealthStatusApiService>());
        Assert.NotNull(provider.GetRequiredService<IRenderDefinitionService>());
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
