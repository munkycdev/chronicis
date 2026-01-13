using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Api.Services.ExternalLinks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Application Insights
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://chronicis.app",
                "https://www.chronicis.app",
                "https://ambitious-mushroom-015091e1e.5.azurestaticapps.net",
                "http://localhost:5001",
                "https://localhost:5001",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Database
builder.Services.AddDbContext<ChronicisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChronicisDb")));

// Current user service (replaces FunctionContext user resolution)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// External links
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("SrdExternalLinks", client =>
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
