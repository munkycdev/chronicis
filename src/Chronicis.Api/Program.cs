using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        // Register global authentication middleware
        builder.UseMiddleware<AuthenticationMiddleware>();
    })
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

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Auth0 Configuration
        services.Configure<Auth0Configuration>(
            configuration.GetSection("Auth0"));

        // Database
        services.AddDbContext<ChronicisDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ChronicisDb")));

        // Services
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IArticleValidationService, ArticleValidationService>();
        services.AddScoped<IAISummaryService, AISummaryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorldService, WorldService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IArcService, ArcService>();
        services.AddScoped<ILinkParser, LinkParser>();
        services.AddScoped<ILinkSyncService, LinkSyncService>();
        services.AddScoped<IAutoLinkService, AutoLinkService>();
        
        // New unified summary service
        services.AddScoped<ISummaryService, SummaryService>();
    })
    .Build();

host.Run();
