using Chronicis.Client;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Diagnostics;

StartLocalDB();

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) 
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Register application services
builder.Services.AddScoped<ArticleApiService>();
builder.Services.AddScoped<TreeStateService>();

await builder.Build().RunAsync();

static void StartLocalDB()
{
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = "start mssqllocaldb",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode == 0 || process.ExitCode == -1) // -1 means already running
        {
            Console.WriteLine("✓ LocalDB is running");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not start LocalDB: {ex.Message}");
        Console.WriteLine("You may need to start it manually: sqllocaldb start mssqllocaldb");
    }
}