using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class FeatCategoryStrategyTests
{
    private readonly FeatCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("feats", _sut.CategoryKey);
        Assert.Equal("feats", _sut.Endpoint);
        Assert.Equal("Feat", _sut.DisplayName);
        Assert.Equal("⭐", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_WithPrerequisite_IncludesItalicLine()
    {
        using var doc = JsonDocument.Parse("""{"prerequisite":"Dex 13","desc":"You're quick."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Alert");
        Assert.Contains("# Alert", md);
        Assert.Contains("*Prerequisite: Dex 13*", md);
        Assert.Contains("You're quick.", md);
    }

    [Fact]
    public void BuildMarkdown_NoPrerequisite_OmitsLine()
    {
        using var doc = JsonDocument.Parse("""{"desc":"Always ready."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Alert");
        Assert.DoesNotContain("Prerequisite", md);
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
        Assert.Equal("Feat", _sut.BuildSubtitle(doc.RootElement));
    }
}
