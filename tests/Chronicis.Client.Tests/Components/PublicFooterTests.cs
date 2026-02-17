using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the PublicFooter component.
/// This component displays the footer with branding, navigation, and copyright.
/// </summary>
[ExcludeFromCodeCoverage]
public class PublicFooterTests : TestContext
{
    [Fact]
    public void PublicFooter_Renders_FooterElement()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var footer = cut.Find("footer");
        Assert.NotNull(footer);
    }

    [Fact]
    public void PublicFooter_Renders_Logo()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var logo = cut.Find("img");
        Assert.Equal("/images/logo.png", logo.GetAttribute("src"));
        Assert.Equal("Chronicis", logo.GetAttribute("alt"));
    }

    [Fact]
    public void PublicFooter_Renders_BrandName()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        Assert.Contains("Chronicis", cut.Markup);
    }

    [Fact]
    public void PublicFooter_Renders_NavigationLinks()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var links = cut.FindAll("nav a");
        Assert.True(links.Count >= 5); // Should have at least 5 nav links
    }

    [Fact]
    public void PublicFooter_HasHomeLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var homeLink = cut.Find("a[href='/']");
        Assert.Equal("Home", homeLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasAboutLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var aboutLink = cut.Find("a[href='/about']");
        Assert.Equal("About", aboutLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasPrivacyLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var privacyLink = cut.Find("a[href='/privacy']");
        Assert.Equal("Privacy Policy", privacyLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasTermsLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var termsLink = cut.Find("a[href='/terms-of-service']");
        Assert.Equal("Terms of Service", termsLink.TextContent);
    }

    [Fact]
    public void PublicFooter_Renders_CopyrightYear()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var currentYear = DateTimeOffset.Now.Year.ToString();
        Assert.Contains(currentYear, cut.Markup);
    }

    [Fact]
    public void PublicFooter_Renders_CopyrightText()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        Assert.Contains("© ", cut.Markup);
        Assert.Contains("Chronicis", cut.Markup);
    }

    [Fact]
    public void PublicFooter_HasFooterClass()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var footer = cut.Find("footer");
        Assert.Contains("chronicis-footer", footer.ClassName);
    }

    [Fact]
    public void PublicFooter_HasNavElement()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var nav = cut.Find("nav");
        Assert.NotNull(nav);
    }

    [Fact]
    public void PublicFooter_HasChangeLogLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var changeLogLink = cut.Find("a[href='/change-log']");
        Assert.Equal("Change Log", changeLogLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasLicensesLink()
    {
        // Act
        var cut = RenderComponent<PublicFooter>();

        // Assert
        var licensesLink = cut.Find("a[href='/licenses']");
        Assert.Equal("Licenses", licensesLink.TextContent);
    }
}
