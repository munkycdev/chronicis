using Blazored.LocalStorage;
using Chronicis.Client;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLogging();

// Determine base URL for redirects (works for both local and Azure)
var baseUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');

// Auth0 Authentication
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = "https://dev-843pl5nrwg3p1xkq.us.auth0.com";
    options.ProviderOptions.ClientId = "Itq22vH9FBHKlYHL1j0A9EgVjA9f6NZQ";
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.RedirectUri = $"{baseUrl}/authentication/login-callback";
    options.ProviderOptions.PostLogoutRedirectUri = baseUrl;
    
    // Auth0 requires the audience parameter to issue a proper JWT access token
    // Without it, Auth0 returns an opaque token that can't be validated
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", "https://api.chronicis.app");

    options.ProviderOptions.DefaultScopes.Clear();
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
})
.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, RemoteUserAccount, CustomUserFactory>();

// MudBlazor configuration
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

// Blazored LocalStorage for persisting user preferences
builder.Services.AddBlazoredLocalStorage();

// Chronicis theme
builder.Services.AddSingleton(CreateChronicisTheme());

// ============================================
// HTTP CLIENT CONFIGURATION (CENTRALIZED)
// ============================================

// For Azure Static Web Apps, the API is at /api (same origin)
// For local dev, use the configured URL
var isAzure = baseUrl.Contains("azurestaticapps.net");
var apiBaseUrl = isAzure 
    ? baseUrl  // Same origin - API is at /api
    : (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071");

// Register the auth handler
builder.Services.AddScoped<ChronicisAuthHandler>();

// Chronicis API client (with auth) - used by all API services
builder.Services.AddHttpClient("ChronicisApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<ChronicisAuthHandler>();

// Quote service (no auth needed - external API)
builder.Services.AddHttpClient<IQuoteService, QuoteService>(client =>
{
    client.BaseAddress = new Uri("https://api.quotable.io/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ============================================
// APPLICATION SERVICES
// ============================================

// API Services - all use the "ChronicisApi" named client
builder.Services.AddScoped<IArticleApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ArticleApiService>>();
    return new ArticleApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<ISearchApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<SearchApiService>>();
    return new SearchApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<IHashtagApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<HashtagApiService>>();
    return new HashtagApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<IAISummaryApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<AISummaryApiService>>();
    return new AISummaryApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<IAutoHashtagApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<AutoHashtagApiService>>();
    return new AutoHashtagApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<IWorldApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<WorldApiService>>();
    return new WorldApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<ICampaignApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<CampaignApiService>>();
    return new CampaignApiService(factory.CreateClient("ChronicisApi"), logger);
});

// State & Auth services
builder.Services.AddScoped<ITreeStateService, TreeStateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppContextService, AppContextService>();

await builder.Build().RunAsync();

// ============================================
// THEME DEFINITION
// ============================================
static MudTheme CreateChronicisTheme() => new MudTheme
{
    PaletteLight = new PaletteLight
    {
        Primary = "#C4AF8E",
        Secondary = "#3A4750",
        Background = "#F4F0EA",
        Surface = "#FFFFFF",
        AppbarBackground = "#1F2A33",
        DrawerBackground = "#1F2A33",
        DrawerText = "#F4F0EA",
        AppbarText = "#F4F0EA",
        TextPrimary = "#1A1A1A",
        TextSecondary = "#3A4750",
        ActionDefault = "#C4AF8E",
        ActionDisabled = "rgba(196, 175, 142, 0.38)",
        ActionDisabledBackground = "rgba(196, 175, 142, 0.12)",
        Divider = "rgba(196, 175, 142, 0.12)",
        DividerLight = "rgba(196, 175, 142, 0.06)",
        TableLines = "rgba(196, 175, 142, 0.12)",
        LinesDefault = "rgba(196, 175, 142, 0.12)",
        LinesInputs = "rgba(196, 175, 142, 0.32)",
        TextDisabled = "rgba(26, 26, 26, 0.38)",
        Success = "#4CAF50",
        Warning = "#FFA726",
        Error = "#ad1412ff",
        Info = "#29B6F6"
    },
    PaletteDark = new PaletteDark
    {
        Primary = "#C4AF8E",
        Secondary = "#3A4750",
        Background = "#1F2A33",
        Surface = "#1F2A33",
        AppbarBackground = "#1A2027",
        DrawerBackground = "#1F2A33",
        DrawerText = "#F4F0EA",
        AppbarText = "#F4F0EA",
        TextPrimary = "#F4F0EA",
        TextSecondary = "#C4AF8E",
        ActionDefault = "#C4AF8E",
        Divider = "rgba(196, 175, 142, 0.12)",
        Success = "#4CAF50",
        Warning = "#FFA726",
        Error = "#ad1412ff",
        Info = "#29B6F6"
    },
    Typography = new Typography
    {
        Default = new Default
        {
            FontFamily = new[] { "Roboto", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif" }
        },
        H1 = new H1 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "2.5rem", FontWeight = 500, LineHeight = 1.2, LetterSpacing = "0.5px" },
        H2 = new H2 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "2rem", FontWeight = 500, LineHeight = 1.3 },
        H3 = new H3 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "1.5rem", FontWeight = 500, LineHeight = 1.4 },
        H4 = new H4 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "1.25rem", FontWeight = 500 },
        H5 = new H5 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "1.125rem", FontWeight = 500 },
        H6 = new H6 { FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" }, FontSize = "1rem", FontWeight = 500 },
        Button = new Button { TextTransform = "none", FontWeight = 500 }
    },
    LayoutProperties = new LayoutProperties
    {
        DrawerWidthLeft = "320px",
        DrawerWidthRight = "320px",
        AppbarHeight = "64px"
    },
    ZIndex = new ZIndex
    {
        Drawer = 1200,
        AppBar = 1100,
        Dialog = 1300,
        Popover = 1400,
        Snackbar = 1500,
        Tooltip = 1600
    }
};

// ============================================
// CUSTOM USER FACTORY FOR AUTH0
// ============================================
public class CustomUserFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    public CustomUserFactory(IAccessTokenProviderAccessor accessor) : base(accessor) { }
}
