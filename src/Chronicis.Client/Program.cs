using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Chronicis.Client;
using Chronicis.Client.Extensions;
using Chronicis.Client.Theme;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddLogging();

        var baseUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');

        // Authentication
        builder.Services.AddChronicisAuthentication(baseUrl);

        // MudBlazor with Chronicis configuration
        builder.Services.AddChronicisMudBlazor();
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddSingleton(ThemeConfig.CreateChronicisTheme());

        // HTTP Clients
        builder.Services.AddChronicisHttpClients(builder.Configuration);

        // Application Services
        builder.Services.AddChronicisApplicationServices(builder.Configuration);

        await builder.Build().RunAsync();
    }
}
