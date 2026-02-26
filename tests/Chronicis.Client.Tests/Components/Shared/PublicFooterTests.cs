using System.Diagnostics.CodeAnalysis;
using System.Net;
using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the PublicFooter component.
/// This component displays the footer with branding, navigation, copyright, and version badge.
/// </summary>
[ExcludeFromCodeCoverage]
public class PublicFooterTests : TestContext
{
    private void RegisterVersionService(string version = "3.0.42", string sha = "abc1234")
    {
        var json = $$"""{"version":"{{version}}","buildNumber":"42","sha":"{{sha}}","buildDate":""}""";
        Services.AddSingleton<IVersionService>(
            new VersionService(
                TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json),
                NullLogger<VersionService>.Instance));
    }

    [Fact]
    public void PublicFooter_Renders_FooterElement()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        Assert.NotNull(cut.Find("footer"));
    }

    [Fact]
    public void PublicFooter_Renders_Logo()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var logo = cut.Find("img");
        Assert.Equal("/images/logo.png", logo.GetAttribute("src"));
        Assert.Equal("Chronicis", logo.GetAttribute("alt"));
    }

    [Fact]
    public void PublicFooter_Renders_BrandName()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        Assert.Contains("Chronicis", cut.Markup);
    }

    [Fact]
    public void PublicFooter_Renders_NavigationLinks()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var links = cut.FindAll("nav a");
        Assert.True(links.Count >= 5);
    }

    [Fact]
    public void PublicFooter_HasHomeLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var homeLink = cut.Find("a[href='/']");
        Assert.Equal("Home", homeLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasAboutLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var aboutLink = cut.Find("a[href='/about']");
        Assert.Equal("About", aboutLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasPrivacyLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var privacyLink = cut.Find("a[href='/privacy']");
        Assert.Equal("Privacy Policy", privacyLink.TextContent);
    }

    [Fact]
    public void PublicFooter_HasTermsLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var termsLink = cut.Find("a[href='/terms-of-service']");
        Assert.Equal("Terms of Service", termsLink.TextContent);
    }

    [Fact]
    public void PublicFooter_Renders_CopyrightYear()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var currentYear = DateTimeOffset.Now.Year.ToString();
        Assert.Contains(currentYear, cut.Markup);
    }

    [Fact]
    public void PublicFooter_Renders_CopyrightText()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        Assert.Contains("© ", cut.Markup);
        Assert.Contains("Chronicis", cut.Markup);
    }

    [Fact]
    public void PublicFooter_HasFooterClass()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var footer = cut.Find("footer");
        Assert.Contains("chronicis-footer", footer.ClassName);
    }

    [Fact]
    public void PublicFooter_HasNavElement()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        Assert.NotNull(cut.Find("nav"));
    }

    [Fact]
    public void PublicFooter_HasChangeLogLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var link = cut.Find("a[href='/change-log']");
        Assert.Equal("Change Log", link.TextContent);
    }

    [Fact]
    public void PublicFooter_HasLicensesLink()
    {
        RegisterVersionService();
        var cut = RenderComponent<PublicFooter>();

        var link = cut.Find("a[href='/licenses']");
        Assert.Equal("Licenses", link.TextContent);
    }

    [Fact]
    public void PublicFooter_ShowsVersionBadge_WhenVersionLoaded()
    {
        RegisterVersionService(version: "3.0.42", sha: "abc1234");
        var cut = RenderComponent<PublicFooter>();

        var badge = cut.Find(".footer-version");
        Assert.Contains("3.0.42", badge.TextContent);
        Assert.Contains("abc1234", badge.GetAttribute("title") ?? string.Empty);
    }

    [Fact]
    public void PublicFooter_HidesVersionBadge_WhenVersionIsEmpty()
    {
        // Simulate fallback: VersionService returns empty string version
        var json = """{"version":"","buildNumber":"0","sha":"","buildDate":""}""";
        Services.AddSingleton<IVersionService>(
            new VersionService(
                TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json),
                NullLogger<VersionService>.Instance));

        var cut = RenderComponent<PublicFooter>();

        Assert.Empty(cut.FindAll(".footer-version"));
    }
}
