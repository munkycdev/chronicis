using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Http;
using MudBlazor;
using MudBlazor.Services;
using Chronicis.Client;
using Chronicis.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLogging();

// HTTP Client for API calls
builder.Services.AddScoped(sp => new HttpClient 
{
    BaseAddress = new Uri("http://localhost:7071")
    //BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// MudBlazor with custom Chronicis theme
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

// Custom Chronicis theme
var chronicisTheme = new MudTheme
{
    PaletteLight = new PaletteLight
    {
        Primary = "#C4AF8E",           // Beige-Gold
        Secondary = "#3A4750",         // Slate Grey
        Background = "#F4F0EA",        // Soft Off-White
        Surface = "#FFFFFF",
        AppbarBackground = "#1F2A33",  // Deep Blue-Grey
        DrawerBackground = "#1F2A33",  // Deep Blue-Grey
        DrawerText = "#F4F0EA",
        AppbarText = "#F4F0EA",
        TextPrimary = "#1A1A1A",       // Charcoal
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
        Error = "#EF5350",
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
        Error = "#EF5350",
        Info = "#29B6F6"
    },
    
    Typography = new Typography
    {
        Default = new Default
        {
            FontFamily = new[] { "Roboto", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif" }
        },
        H1 = new H1
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "2.5rem",
            FontWeight = 500,
            LineHeight = 1.2,
            LetterSpacing = "0.5px"
        },
        H2 = new H2
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "2rem",
            FontWeight = 500,
            LineHeight = 1.3
        },
        H3 = new H3
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "1.5rem",
            FontWeight = 500,
            LineHeight = 1.4
        },
        H4 = new H4
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "1.25rem",
            FontWeight = 500
        },
        H5 = new H5
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "1.125rem",
            FontWeight = 500
        },
        H6 = new H6
        {
            FontFamily = new[] { "Spellweaver Display", "Georgia", "serif" },
            FontSize = "1rem",
            FontWeight = 500
        },
        Button = new Button
        {
            TextTransform = "none",
            FontWeight = 500
        }
    },
    
    Shadows = new Shadow
    {
        Elevation = new string[]
        {
            "none",
            "0 2px 4px rgba(0, 0, 0, 0.1)",      // 1
            "0 4px 8px rgba(0, 0, 0, 0.15)",     // 2
            "0 8px 16px rgba(0, 0, 0, 0.2)",     // 3
            "0 12px 24px rgba(0, 0, 0, 0.25)",   // 4
            "0 16px 32px rgba(0, 0, 0, 0.3)",    // 5
            // ... continue as needed
        }
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

// Apply the theme
builder.Services.AddSingleton(chronicisTheme);

// Application Services
builder.Services.AddScoped<IArticleApiService, ArticleApiService>();
builder.Services.AddScoped<ITreeStateService, TreeStateService>();
builder.Services.AddScoped<IHashtagApiService, HashtagApiService>();

builder.Services.AddHttpClient<IQuoteService, QuoteService>(client =>
{
    client.BaseAddress = new Uri("https://api.quotable.io/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

await builder.Build().RunAsync();
