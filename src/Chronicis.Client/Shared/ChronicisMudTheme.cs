using MudBlazor;

namespace Chronicis.Client.Themes;

public class ChronicisMudTheme : MudTheme
{
    public ChronicisMudTheme()
    {
        string Color_DarkBlueGrey = "#2e3944";
        string Color_LightBlueGrey = "#303943";
        string Color_Parchment = "#e5d9c5";
        string[] Font_Heading = ["Spellcaster", "Garamond", "serif"];
        string[] Font_Body = ["ScalaSans", "Montserrat", "Candara", "sans-serif"];

        PaletteLight = new PaletteLight()
        {
            Primary = Color_Parchment,
            PrimaryDarken = Color_DarkBlueGrey,
            Secondary = Color_DarkBlueGrey,
            Tertiary = Color_LightBlueGrey,
            
            Background = Color_Parchment,
            AppbarText = Color_Parchment,
            DrawerText = Color_Parchment,
            LinesInputs = Color_Parchment,
            DrawerIcon = Color_Parchment,
            Surface = Color_Parchment,

            TextPrimary = Color_DarkBlueGrey,
            AppbarBackground = Color_DarkBlueGrey,

            DrawerBackground = Color_LightBlueGrey,
            TextDisabled = Color_LightBlueGrey,
            
            Error = Colors.Red.Darken2
        };

        Typography = new Typography()
        {
            Default = new Default() { FontSize = "0.95rem", FontFamily = Font_Body, FontWeight = 500 },

            H1 = new H1() { FontSize = "3rem", FontFamily = Font_Heading },
            H2 = new H2() { FontSize = "2.5rem", FontFamily = Font_Heading },
            H3 = new H3() { FontSize = "2rem", FontFamily = Font_Heading },
            H4 = new H4() { FontSize = "1.75rem", FontFamily = Font_Heading },
            H5 = new H5() { FontSize = "1.5rem", FontFamily = Font_Heading },
            H6 = new H6() { FontSize = "1.25rem", FontFamily = Font_Heading },            

            Body1 = new Body1() { FontSize = "0.95rem", FontFamily = Font_Body, TextTransform = "uppercase" },
            Body2 = new Body2() { FontSize = "0.9rem", FontFamily = Font_Body },
            Button = new Button() { FontSize = "0.9rem", FontFamily = Font_Body, FontWeight = 600 },
            Caption = new Caption() { FontSize = "0.8rem", FontFamily = Font_Body },
            Input = new Input() { FontSize = "0.95rem", FontFamily = Font_Body },
            Subtitle1 = new Subtitle1() { FontSize = "1rem", FontFamily = Font_Body },
            Subtitle2 = new Subtitle2() { FontSize = "0.875rem", FontFamily = Font_Body },
            Overline = new Overline() { FontSize = "0.75rem", FontFamily = Font_Body }
        };

        PseudoCss = new PseudoCss()
        {
        };
    }
}