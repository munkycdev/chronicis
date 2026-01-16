using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Api.Services.ExternalLinks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for DataDog
builder.Host.UseSerilog((context, services, configuration) =>
{
    var datadogApiKey = context.Configuration["DataDog:ApiKey"];
    var datadogSite = context.Configuration["DataDog:Site"] ?? "datadoghq.com";
    var serviceName = context.Configuration["DataDog:ServiceName"] ?? "chronicis-api";
    var environment = context.HostingEnvironment.EnvironmentName;
    
    configuration
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", serviceName)
        .Enrich.WithProperty("env", environment)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
        .WriteTo.Console();

    // Only configure DataDog sink if API key is present
    if (!string.IsNullOrWhiteSpace(datadogApiKey))
    {
        configuration.WriteTo.DatadogLogs(
            datadogApiKey,
            source: "csharp",
            service: serviceName,
            host: Environment.MachineName,
            tags: new[] { $"env:{environment}" },
            configuration: new DatadogConfiguration { Url = $"https://http-intake.logs.{datadogSite}" }
        );
    }
});

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

// Services
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IArticleValidationService, ArticleValidationService>();
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

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
