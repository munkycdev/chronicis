using MudBlazor;

namespace Chronicis.Client.Theme;

/// <summary>
/// Provides the Chronicis application theme configuration for MudBlazor.
/// </summary>
public static class ThemeConfig
{
    /// <summary>
    /// Creates and returns the Chronicis theme with custom colors, typography, and layout properties.
    /// </summary>
    public static MudTheme CreateChronicisTheme() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#3A4750",      // Slate blue-grey
            Secondary = "#C4AF8E",    // Beige-gold
            Background = "#F4F0EA",
            Surface = "#FFFFFF",
            AppbarBackground = "#1F2A33",
            DrawerBackground = "#1F2A33",
            DrawerText = "#F4F0EA",
            AppbarText = "#F4F0EA",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#3A4750",
            ActionDefault = "#3A4750",
            ActionDisabled = "rgba(58, 71, 80, 0.38)",
            ActionDisabledBackground = "rgba(58, 71, 80, 0.12)",
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
            Primary = "#F4F0EA",      // Off-white
            Secondary = "#C4AF8E",    // Beige-gold
            Background = "#1F2A33",
            Surface = "#1F2A33",
            AppbarBackground = "#1A2027",
            DrawerBackground = "#1F2A33",
            DrawerText = "#F4F0EA",
            AppbarText = "#F4F0EA",
            TextPrimary = "#F4F0EA",
            TextSecondary = "#C4AF8E",
            ActionDefault = "#F4F0EA",
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
}
