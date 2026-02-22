using Chronicis.Client.Extensions;
using Chronicis.Client.Services;
using Chronicis.Shared.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class ApplicationServiceExtensionsTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SysAdmin:Auth0UserIds:0"] = "oauth2|discord|123",
                ["SysAdmin:Emails:0"] = "admin@example.com",
            })
            .Build();

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

        var returned = services.AddChronicisApplicationServices(BuildConfig());

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IArticleApiService>());
        Assert.NotNull(provider.GetRequiredService<IQuestApiService>());
        Assert.NotNull(provider.GetRequiredService<IExportApiService>());
        Assert.NotNull(provider.GetRequiredService<IPublicApiService>());
        Assert.NotNull(provider.GetRequiredService<IHealthStatusApiService>());
        Assert.NotNull(provider.GetRequiredService<IRenderDefinitionService>());
        Assert.NotNull(provider.GetRequiredService<IAdminApiService>());
        Assert.NotNull(provider.GetRequiredService<ISysAdminChecker>());
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
