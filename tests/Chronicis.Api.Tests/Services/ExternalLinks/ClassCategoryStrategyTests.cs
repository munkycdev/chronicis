using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class ClassCategoryStrategyTests
{
    private readonly ClassCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("classes", _sut.CategoryKey);
        Assert.Equal("classes", _sut.Endpoint);
        Assert.Equal("Class", _sut.DisplayName);
        Assert.Equal("⚔️", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_WithHitDie_IncludesField()
    {
        using var doc = JsonDocument.Parse("""{"hit_dice":"1d6","desc":"A scholarly magic-user."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Wizard");
        Assert.Contains("# Wizard", md);
        Assert.Contains("**Hit Die:** 1d6", md);
        Assert.Contains("A scholarly magic-user.", md);
    }

    [Fact]
    public void BuildMarkdown_NoHitDie_OmitsField()
    {
        using var doc = JsonDocument.Parse("""{"desc":"Custom class."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.DoesNotContain("Hit Die", md);
    }

    [Fact]
    public void BuildMarkdown_NoDesc_JustTitle()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.Contains("# Custom", md);
    }

    [Fact]
    public void BuildSubtitle_ReturnsDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Class", _sut.BuildSubtitle(doc.RootElement));
    }
}
