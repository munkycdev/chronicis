using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class SpellCategoryStrategyTests
{
    private readonly SpellCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("spells", _sut.CategoryKey);
        Assert.Equal("spells", _sut.Endpoint);
        Assert.Equal("Spell", _sut.DisplayName);
        Assert.Equal("✨", _sut.Icon);
        Assert.Equal("5e-2014", _sut.DocumentSlug);
        Assert.Equal("spells", _sut.WebCategory);
    }

    [Fact]
    public void BuildMarkdown_FullSpell_IncludesAllSections()
    {
        using var doc = JsonDocument.Parse("""
        {
          "name": "Fireball",
          "level": 3,
          "school": { "name": "Evocation" },
          "casting_time": "1 action",
          "range_text": "150 feet",
          "duration": "Instantaneous",
          "concentration": false,
          "ritual": false,
          "verbal": true,
          "somatic": true,
          "material": true,
          "material_specified": "a tiny ball of bat guano",
          "desc": "A bright streak flashes.",
          "higher_level": "Increases by 1d6 per level."
        }
        """);

        var md = _sut.BuildMarkdown(doc.RootElement, "Fireball");
        Assert.Contains("# Fireball", md);
        Assert.Contains("*Level 3 Evocation*", md);
        Assert.Contains("## Casting", md);
        Assert.Contains("- **Casting Time:** 1 action", md);
        Assert.Contains("- **Range:** 150 feet", md);
        Assert.Contains("- **Duration:** Instantaneous", md);
        Assert.Contains("- **Components:** V, S, M (a tiny ball of bat guano)", md);
        Assert.Contains("## Description", md);
        Assert.Contains("## At Higher Levels", md);
    }

    [Fact]
    public void BuildMarkdown_Cantrip_WithoutSchool_ShowsCantripOnly()
    {
        using var doc = JsonDocument.Parse("""{"level":0}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Spark");
        Assert.Contains("*Cantrip*", md);
    }

    [Fact]
    public void BuildMarkdown_RitualAndConcentration_ShowsBoth()
    {
        using var doc = JsonDocument.Parse("""
        {
          "level": 2,
          "casting_time": "1 action",
          "duration": "8 hours",
          "concentration": true,
          "ritual": true
        }
        """);

        var md = _sut.BuildMarkdown(doc.RootElement, "Aid");
        Assert.Contains("(ritual)", md);
        Assert.Contains("Concentration, 8 hours", md);
    }

    [Fact]
    public void BuildMarkdown_MaterialOnly_NoParentheses()
    {
        using var doc = JsonDocument.Parse("""{"level":1,"material":true}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Spell");
        Assert.Contains("- **Components:** M", md);
        Assert.DoesNotContain("()", md);
    }

    [Fact]
    public void BuildMarkdown_FallbackRange_UsesRangeField()
    {
        using var doc = JsonDocument.Parse("""{"level":1,"range":"Self"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Shield");
        Assert.Contains("- **Range:** Self", md);
    }

    [Fact]
    public void BuildSubtitle_Level0_ShowsCantrip()
    {
        using var doc = JsonDocument.Parse("""{"level":0,"school":{"name":"Evocation"}}""");
        Assert.Equal("Spell • Cantrip • Evocation", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_Level3_ShowsLevel()
    {
        using var doc = JsonDocument.Parse("""{"level":3}""");
        Assert.Equal("Spell • Level 3", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_NoLevelOrSchool_JustDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Spell", _sut.BuildSubtitle(doc.RootElement));
    }
}
