using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

StartLocalDB();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
        });

        // Database configuration
        var connectionString = context.Configuration.GetConnectionString("ChronicisDb")
            ?? throw new InvalidOperationException("Connection string 'ChronicisDb' not found.");

        services.AddDbContext<ChronicisDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register services
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ArticleValidationService>();
    })
    .Build();

host.Run();

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