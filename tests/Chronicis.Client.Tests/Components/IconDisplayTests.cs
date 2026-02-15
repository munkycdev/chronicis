using Bunit;
using Chronicis.Client.Components.Shared;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the IconDisplay component.
/// This component renders icons (emojis or Font Awesome) with fallback support.
/// </summary>
public class IconDisplayTests : TestContext
{
    [Fact]
    public void IconDisplay_WithEmoji_RendersSpanWithEmoji()
    {
        // Arrange
        var emoji = "🐉";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, emoji));

        // Assert
        var span = cut.Find("span");
        Assert.Contains(emoji, span.TextContent);
    }

    [Fact]
    public void IconDisplay_WithFontAwesomeIcon_RendersITag()
    {
        // Arrange
        var fontAwesomeClass = "fa-solid fa-dragon";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, fontAwesomeClass));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("fa-solid", icon.ClassName);
        Assert.Contains("fa-dragon", icon.ClassName);
    }

    [Fact]
    public void IconDisplay_WithNullIcon_UsesDefaultIcon()
    {
        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, (string?)null));

        // Assert - Default is "👤"
        var span = cut.Find("span");
        Assert.Contains("👤", span.TextContent);
    }

    [Fact]
    public void IconDisplay_WithEmptyIcon_UsesDefaultIcon()
    {
        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, string.Empty));

        // Assert - Default is "👤"
        var span = cut.Find("span");
        Assert.Contains("👤", span.TextContent);
    }

    [Fact]
    public void IconDisplay_WithCustomDefaultIcon_UsesCustomDefault()
    {
        // Arrange
        var customDefault = "⚔️";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, (string?)null)
            .Add(p => p.DefaultIcon, customDefault));

        // Assert
        var span = cut.Find("span");
        Assert.Contains(customDefault, span.TextContent);
    }

    [Fact]
    public void IconDisplay_WithFontAwesomeDefaultIcon_RendersITag()
    {
        // Arrange
        var defaultFontAwesome = "fa-solid fa-user";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, (string?)null)
            .Add(p => p.DefaultIcon, defaultFontAwesome));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("fa-solid", icon.ClassName);
        Assert.Contains("fa-user", icon.ClassName);
    }

    [Fact]
    public void IconDisplay_WithCssClass_AppliesClass()
    {
        // Arrange
        var emoji = "🎮";
        var cssClass = "custom-icon-class";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, emoji)
            .Add(p => p.CssClass, cssClass));

        // Assert
        var span = cut.Find("span");
        Assert.Contains(cssClass, span.ClassName);
    }

    [Fact]
    public void IconDisplay_WithStyle_AppliesStyle()
    {
        // Arrange
        var emoji = "🎮";
        var style = "font-size: 2rem;";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, emoji)
            .Add(p => p.Style, style));

        // Assert
        var span = cut.Find("span");
        Assert.Contains(style, span.GetAttribute("style"));
    }

    [Fact]
    public void IconDisplay_WithFontAwesomeAndCssClass_CombinesClasses()
    {
        // Arrange
        var fontAwesomeClass = "fa-solid fa-shield";
        var additionalClass = "text-primary";

        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, fontAwesomeClass)
            .Add(p => p.CssClass, additionalClass));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("fa-solid", icon.ClassName);
        Assert.Contains("fa-shield", icon.ClassName);
        Assert.Contains(additionalClass, icon.ClassName);
    }

    [Theory]
    [InlineData("🎲", true)]  // Emoji
    [InlineData("⚔️", true)]  // Emoji
    [InlineData("fa-solid fa-dice", false)]  // Font Awesome
    [InlineData("fa-regular fa-sword", false)]  // Font Awesome
    public void IconDisplay_DetectsIconTypeCorrectly(string icon, bool shouldBeSpan)
    {
        // Act
        var cut = RenderComponent<IconDisplay>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        if (shouldBeSpan)
        {
            var span = cut.Find("span");
            Assert.NotNull(span);
        }
        else
        {
            var i = cut.Find("i");
            Assert.NotNull(i);
        }
    }
}
