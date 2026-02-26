using Chronicis.Client.Services;
using Chronicis.Shared.Enums;
using System.Reflection;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class TutorialPageTypeResolverTests
{
    private readonly TutorialPageTypeResolver _sut = new();

    [Fact]
    public void Resolve_WhenUriInvalid_ReturnsPageDefault()
    {
        Assert.Equal("Page:Default", _sut.Resolve("   "));
    }

    [Fact]
    public void Resolve_WhenRootPath_ReturnsPageDefault()
    {
        Assert.Equal("Page:Default", _sut.Resolve("/"));
        Assert.Equal("Page:Default", _sut.Resolve("https://example.com/"));
    }

    [Fact]
    public void Resolve_WhenArticleRoute_UsesArticleTypeOrAny()
    {
        Assert.Equal("ArticleType:Character", _sut.Resolve("/article/world/hero", ArticleType.Character));
        Assert.Equal("ArticleType:Any", _sut.Resolve("/article/world/hero"));
    }

    [Theory]
    [InlineData("/dashboard", "Page:Dashboard")]
    [InlineData("/settings", "Page:Settings")]
    [InlineData("/world/123", "Page:WorldDetail")]
    [InlineData("/campaign/123", "Page:CampaignDetail")]
    [InlineData("/arc/123", "Page:ArcDetail")]
    [InlineData("/session/123", "Page:SessionDetail")]
    [InlineData("/search?q=dragon", "Page:Search")]
    [InlineData("/cosmos", "Page:Cosmos")]
    [InlineData("/about", "Page:About")]
    [InlineData("/getting-started", "Page:GettingStarted")]
    [InlineData("/changelog", "Page:ChangeLog")]
    [InlineData("/change-log", "Page:ChangeLog")]
    [InlineData("/privacy", "Page:Privacy")]
    [InlineData("/terms", "Page:Terms")]
    [InlineData("/licenses", "Page:Licenses")]
    [InlineData("/custom-page", "Page:CustomPage")]
    [InlineData("custom_page", "Page:CustomPage")]
    public void Resolve_WhenPageRoute_ResolvesExpectedPageType(string uri, string expected)
    {
        Assert.Equal(expected, _sut.Resolve(uri, worldId: Guid.NewGuid(), isTutorialWorld: true));
    }

    [Theory]
    [InlineData("/admin/status", "Page:AdminStatus")]
    [InlineData("/admin/utilities", "Page:AdminUtilities")]
    [InlineData("/admin/custom-tools", "Page:AdminCustomTools")]
    [InlineData("/admin", "Page:Admin")]
    public void Resolve_WhenAdminRoute_ResolvesExpectedPageType(string uri, string expected)
    {
        Assert.Equal(expected, _sut.Resolve(uri));
    }

    [Fact]
    public void ResolvePageName_WhenNoSegments_ReturnsDefault()
    {
        var method = typeof(TutorialPageTypeResolver).GetMethod("ResolvePageName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (string?)method!.Invoke(null, [Array.Empty<string>()]);

        Assert.Equal("Default", result);
    }

    [Fact]
    public void TryGetPathSegments_WhenUriCannotBeParsed_ReturnsFalse()
    {
        var method = typeof(TutorialPageTypeResolver).GetMethod("TryGetPathSegments", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        object?[] args = [new string('a', 70000), null];

        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
        Assert.NotNull(args[1]);
        Assert.Empty((string[])args[1]!);
    }

    [Fact]
    public void ToPascalCase_WhenWhitespaceOrDelimitersOnly_ReturnsDefault()
    {
        var method = typeof(TutorialPageTypeResolver).GetMethod("ToPascalCase", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var whitespace = (string?)method!.Invoke(null, ["   "]);
        var delimitersOnly = (string?)method!.Invoke(null, ["---___   "]);

        Assert.Equal("Default", whitespace);
        Assert.Equal("Default", delimitersOnly);
    }

    [Fact]
    public void ToPascalWord_WhenEmptyOrSingleChar_CoversEdgeCases()
    {
        var method = typeof(TutorialPageTypeResolver).GetMethod("ToPascalWord", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var empty = (string?)method!.Invoke(null, [""]);
        var single = (string?)method!.Invoke(null, ["x"]);

        Assert.Equal(string.Empty, empty);
        Assert.Equal("X", single);
    }
}
