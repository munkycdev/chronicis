using Bunit;
using Chronicis.Client.Components.Routing;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the AuthorizingScreen component.
/// This component displays a loading screen while authentication state is determined.
/// </summary>
public class AuthorizingScreenTests : TestContext
{
    [Fact]
    public void AuthorizingScreen_Renders_LoadingContainer()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var container = cut.Find(".chronicis-loading-container");
        Assert.NotNull(container);
    }

    [Fact]
    public void AuthorizingScreen_Renders_Logo()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var logo = cut.Find("img");
        Assert.Equal("/images/logo.png", logo.GetAttribute("src"));
        Assert.Equal("Chronicis", logo.GetAttribute("alt"));
    }

    [Fact]
    public void AuthorizingScreen_Renders_LoadingText()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var heading = cut.Find("h1.chronicis-loading-text");
        Assert.Contains("Loading Chronicis", heading.TextContent);
    }

    [Fact]
    public void AuthorizingScreen_Renders_Subtext()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var subtext = cut.Find("h2.chronicis-loading-subtext");
        Assert.Contains("Your adventures deserve stories", subtext.TextContent);
    }

    [Fact]
    public void AuthorizingScreen_HasLoadingScreenClass()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var screen = cut.Find(".chronicis-loading-screen");
        Assert.NotNull(screen);
    }

    [Fact]
    public void AuthorizingScreen_Logo_HasCorrectDimensions()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var logo = cut.Find("img");
        Assert.Equal("250", logo.GetAttribute("height"));
        Assert.Equal("250", logo.GetAttribute("width"));
    }

    [Fact]
    public void AuthorizingScreen_Logo_HasAnimationClass()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var logoSpan = cut.Find(".chronicis-logo-animated");
        Assert.NotNull(logoSpan);
    }

    [Fact]
    public void AuthorizingScreen_Logo_HasPulseClass()
    {
        // Act
        var cut = RenderComponent<AuthorizingScreen>();

        // Assert
        var logo = cut.Find("img.chronicis-logo-pulse");
        Assert.NotNull(logo);
    }
}
