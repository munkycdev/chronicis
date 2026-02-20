using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class MagicItemCategoryStrategyTests
{
    private readonly MagicItemCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("magicitems", _sut.CategoryKey);
        Assert.Equal("items", _sut.Endpoint);
        Assert.Equal("Magic Item", _sut.DisplayName);
        Assert.Equal("💎", _sut.Icon);
        Assert.Equal("magic-items", _sut.WebCategory);
    }

    [Fact]
    public void BuildMarkdown_WithAttunement_IncludesParens()
    {
        using var doc = JsonDocument.Parse("""
        {"name":"Ring","type":"Ring","rarity":"Rare","requires_attunement":"requires attunement","desc":"Shiny."}
        """);
        var md = _sut.BuildMarkdown(doc.RootElement, "Ring");
        Assert.Contains("*Ring, Rare (requires attunement)*", md);
        Assert.Contains("Shiny.", md);
    }

    [Fact]
    public void BuildMarkdown_AttunementFalse_NoParens()
    {
        using var doc = JsonDocument.Parse("""{"type":"Wand","rarity":"Uncommon","requires_attunement":"false","desc":"d."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Wand");
        Assert.Contains("*Wand, Uncommon*", md);
        Assert.DoesNotContain("(", md);
    }

    [Fact]
    public void BuildMarkdown_NoSubtitleFields_JustTitle()
    {
        using var doc = JsonDocument.Parse("""{"desc":"Glows."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Orb");
        Assert.Contains("# Orb", md);
        Assert.DoesNotContain("*,*", md);
    }

    [Fact]
    public void BuildSubtitle_WithRarityAndType()
    {
        using var doc = JsonDocument.Parse("""{"rarity":"Uncommon","type":"Wand"}""");
        Assert.Equal("Magic Item • Uncommon • Wand", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_NoFields_JustDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Magic Item", _sut.BuildSubtitle(doc.RootElement));
    }
}
