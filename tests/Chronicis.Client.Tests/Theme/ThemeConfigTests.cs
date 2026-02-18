using Chronicis.Client.Theme;
using Xunit;

namespace Chronicis.Client.Tests.Theme;

public class ThemeConfigTests
{
    [Fact]
    public void CreateChronicisTheme_ReturnsExpectedThemeConfiguration()
    {
        var theme = ThemeConfig.CreateChronicisTheme();

        Assert.NotNull(theme);
        Assert.Equal("#3A4750", theme.PaletteLight.Primary);
        Assert.Equal("#1F2A33", theme.PaletteDark.Background);
        Assert.Equal("320px", theme.LayoutProperties.DrawerWidthLeft);
        Assert.Equal(1600, theme.ZIndex.Tooltip);
        Assert.Equal("Spellweaver Display", theme.Typography.H1.FontFamily[0]);
        Assert.Equal("none", theme.Typography.Button.TextTransform);
    }
}

