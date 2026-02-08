using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Repositories;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Api.Services.ExternalLinks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Bootstrap logger for startup errors (before host is built)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Chronicis API");
    
    // Log Datadog tracer state (read-only - tracer is auto-configured via DD_* env vars)
    DatadogDiagnostics.LogTracerState(Log.Logger);

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName());

    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("ContentRootPath: {ContentRootPath}", builder.Environment.ContentRootPath);

    Log.Information("Adding services...");

    // Add controllers
    builder.Services.AddControllers();

    // Application Insights (TO BE REMOVED IN LATER PHASE)
    builder.Services.AddApplicationInsightsTelemetry();

    // Auth0 JWT Bearer Authentication
    var auth0Config = builder.Configuration.GetSection("Auth0").Get<Auth0Configuration>()
        ?? throw new InvalidOperationException("Auth0 configuration is missing");

    builder.Services.Configure<Auth0Configuration>(builder.Configuration.GetSection("Auth0"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Config.Domain}/";
            options.Audience = auth0Config.Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Config.Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Config.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });

    builder.Services.AddAuthorization();

    // CORS - Updated for separate client App Service
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    // Production origins
                    "https://chronicis.app",
                    "https://www.chronicis.app",
                    "https://chronicis-client.azurewebsites.net",  // New App Service
                    // Legacy Static Web App (will be removed in Phase 14)
                    "https://ambitious-mushroom-015091e1e.5.azurestaticapps.net",
                    // Local development
                    "http://localhost:5001",
                    "https://localhost:5001",
                    "http://localhost:5173",
                    "http://localhost:5000",  // Default Kestrel port
                    "https://localhost:5002"  // Kestrel HTTPS port
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Database with Azure SQL resiliency
    var connectionString = builder.Configuration.GetConnectionString("ChronicisDb");

    // Ensure MARS is enabled (required for EF Core with Azure SQL)
    if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = connectionString.TrimEnd(';') + ";MultipleActiveResultSets=True;";
    }

    builder.Services.AddDbContext<ChronicisDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                // Enable retry on transient failures (Azure SQL best practice)
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                // Set command timeout for long-running queries
                sqlOptions.CommandTimeout(30);
            }));

    // Current user service (replaces FunctionContext user resolution)
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // External links
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient(); // General-purpose HttpClient for diagnostics
    builder.Services.AddHttpClient("Open5eApi", client =>
    {
        var baseUrl = builder.Configuration.GetValue<string>("ExternalLinks:Open5e:BaseUrl");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }
    }).RemoveAllLoggers();
    builder.Services.AddScoped<IExternalLinkProviderRegistry, ExternalLinkProviderRegistry>();
    builder.Services.AddScoped<ExternalLinkSuggestionService>();
    builder.Services.AddScoped<ExternalLinkContentService>();
    builder.Services.AddScoped<ExternalLinkValidationService>();
    builder.Services.AddScoped<IExternalLinkProvider, Open5eExternalLinkProvider>();

    var srd14Config = builder.Configuration.GetSection("ExternalLinks:BlobProviders:Srd14");
    builder.Services.Configure<BlobExternalLinkProviderOptions>(
        "srd14",
        srd14Config
        );

    var srd24Config = builder.Configuration.GetSection("ExternalLinks:BlobProviders:Srd24");
    builder.Services.Configure<BlobExternalLinkProviderOptions>(
        "srd24",
        srd24Config);

    var rosConfig = builder.Configuration.GetSection("ExternalLinks:BlobProviders:Ros");
    builder.Services.Configure<BlobExternalLinkProviderOptions>(
        "ros",
        rosConfig);

    // Register srd14 provider (each provider gets its own connection string and blob client)
    builder.Services.AddScoped<IExternalLinkProvider>(sp =>
    {
        var optionsSnapshot = sp.GetRequiredService<IOptionsSnapshot<BlobExternalLinkProviderOptions>>();
        var options = optionsSnapshot.Get("srd14");
        var cache = sp.GetRequiredService<IMemoryCache>();
        var logger = sp.GetRequiredService<ILogger<BlobExternalLinkProvider>>();
        
        return new BlobExternalLinkProvider(options, cache, logger);
    });

    // Register srd24 provider (decoupled from srd14, uses its own connection string)
    builder.Services.AddScoped<IExternalLinkProvider>(sp =>
    {
        var optionsSnapshot = sp.GetRequiredService<IOptionsSnapshot<BlobExternalLinkProviderOptions>>();
        var options = optionsSnapshot.Get("srd24");
        var cache = sp.GetRequiredService<IMemoryCache>();
        var logger = sp.GetRequiredService<ILogger<BlobExternalLinkProvider>>();
        
        return new BlobExternalLinkProvider(options, cache, logger);
    });

    // Register ros provider (Ruins of Symbaroum)
    builder.Services.AddScoped<IExternalLinkProvider>(sp =>
    {
        var optionsSnapshot = sp.GetRequiredService<IOptionsSnapshot<BlobExternalLinkProviderOptions>>();
        var options = optionsSnapshot.Get("ros");
        var cache = sp.GetRequiredService<IMemoryCache>();
        var logger = sp.GetRequiredService<ILogger<BlobExternalLinkProvider>>();
        
        return new BlobExternalLinkProvider(options, cache, logger);
    });

    // Services
    builder.Services.AddScoped<IArticleService, ArticleService>();
    builder.Services.AddScoped<IArticleValidationService, ArticleValidationService>();
    builder.Services.AddScoped<IArticleExternalLinkService, ArticleExternalLinkService>();
    builder.Services.AddScoped<ISummaryService, SummaryService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IWorldService, WorldService>();
    builder.Services.AddScoped<ICampaignService, CampaignService>();
    builder.Services.AddScoped<IArcService, ArcService>();
    builder.Services.AddScoped<ILinkParser, Chronicis.Api.Services.LinkParser>();
    builder.Services.AddScoped<ILinkSyncService, LinkSyncService>();
    builder.Services.AddScoped<IAutoLinkService, AutoLinkService>();
    builder.Services.AddScoped<IPromptService, PromptService>();
    builder.Services.AddScoped<IPublicWorldService, PublicWorldService>();
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
    builder.Services.AddScoped<IWorldDocumentService, WorldDocumentService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IResourceProviderService, ResourceProviderService>();
    builder.Services.AddScoped<IQuestService, QuestService>();
    builder.Services.AddScoped<IQuestUpdateService, QuestUpdateService>();

    // Repositories
    builder.Services.AddScoped<IResourceProviderRepository, ResourceProviderRepository>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
