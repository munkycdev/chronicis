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
        services.AddScoped<IHashtagParser, HashtagParser>();
        services.AddScoped<IHashtagSyncService, HashtagSyncService>();
    })
    .Build();

host.Run();