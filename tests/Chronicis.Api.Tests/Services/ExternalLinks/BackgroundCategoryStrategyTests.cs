using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class BackgroundCategoryStrategyTests
{
    private readonly BackgroundCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("backgrounds", _sut.CategoryKey);
        Assert.Equal("backgrounds", _sut.Endpoint);
        Assert.Equal("Background", _sut.DisplayName);
        Assert.Equal("📜", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_FullBackground_IncludesAllSections()
    {
        using var doc = JsonDocument.Parse("""
        {
          "desc": "You have spent your life in service.",
          "skill_proficiencies": "Insight, Religion",
          "equipment": "Holy symbol, prayer book",
          "feature": "Shelter of the Faithful",
          "feature_desc": "Free healing at a temple."
        }
        """);
        var md = _sut.BuildMarkdown(doc.RootElement, "Acolyte");
        Assert.Contains("# Acolyte", md);
        Assert.Contains("You have spent your life in service.", md);
        Assert.Contains("**Skill Proficiencies:** Insight, Religion", md);
        Assert.Contains("**Equipment:** Holy symbol, prayer book", md);
        Assert.Contains("## Shelter of the Faithful", md);
        Assert.Contains("Free healing at a temple.", md);
    }

    [Fact]
    public void BuildMarkdown_FeatureWithoutDesc_ShowsHeaderOnly()
    {
        using var doc = JsonDocument.Parse("""{"feature":"Some Feature"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "BG");
        Assert.Contains("## Some Feature", md);
    }

    [Fact]
    public void BuildMarkdown_MinimalBackground_JustTitle()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.Contains("# Custom", md);
    }

    [Fact]
    public void BuildSubtitle_ReturnsDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Background", _sut.BuildSubtitle(doc.RootElement));
    }
}
