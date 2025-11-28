using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Register IConfiguration so it can be injected
        services.AddSingleton<IConfiguration>(configuration);

        // Database
        services.AddDbContext<ChronicisDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ChronicisDb")));

        // Your services
        services.AddScoped<IHashtagParser, HashtagParser>();
        services.AddScoped<IHashtagSyncService, HashtagSyncService>();
        services.AddScoped<IAISummaryService, AISummaryService>();

        // Application Insights (if you have it)
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();