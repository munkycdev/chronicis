using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        // Application Insights (if you have it)
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Auth0 Configuration
        services.Configure<Auth0Configuration>(
            configuration.GetSection("Auth0"));

        // Database
        services.AddDbContext<ChronicisDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ChronicisDb")));

        // Your services
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ArticleValidationService>();
        services.AddScoped<IHashtagParser, HashtagParser>();
        services.AddScoped<IHashtagSyncService, HashtagSyncService>();
        services.AddScoped<IAISummaryService, AISummaryService>();
        services.AddScoped<IUserService, UserService>();
    })
    .Build();

host.Run();