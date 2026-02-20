using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class MonsterCategoryStrategyTests
{
    private readonly MonsterCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("monsters", _sut.CategoryKey);
        Assert.Equal("creatures", _sut.Endpoint);
        Assert.Equal("Monster", _sut.DisplayName);
        Assert.Equal("🐉", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_FullMonster_IncludesAllSections()
    {
        using var doc = JsonDocument.Parse("""
        {
          "name": "Hydra",
          "size": { "name": "Huge" },
          "type": { "name": "Monstrosity" },
          "alignment": "unaligned",
          "armor_class": "15",
          "hit_points": "172",
          "hit_dice": "15d12+75",
          "challenge_rating": "8",
          "speed": { "walk": "30 ft", "swim": "30 ft" },
          "actions": [{ "name": "Bite", "desc": "Attack." }],
          "special_abilities": [{ "name": "Hold Breath", "desc": "1 hour." }],
          "legendary_actions": [{ "name": "Regrow", "desc": "Regrows head." }]
        }
        """);

        var md = _sut.BuildMarkdown(doc.RootElement, "Hydra");
        Assert.Contains("# Hydra", md);
        Assert.Contains("*Huge Monstrosity, unaligned*", md);
        Assert.Contains("## Statistics", md);
        Assert.Contains("**Armor Class:** 15", md);
        Assert.Contains("**Hit Points:** 172 (15d12+75)", md);
        Assert.Contains("**Challenge:** 8", md);
        Assert.Contains("**Speed:** walk 30 ft, swim 30 ft", md);
        Assert.Contains("## Actions", md);
        Assert.Contains("## Special Abilities", md);
        Assert.Contains("## Legendary Actions", md);
    }

    [Fact]
    public void BuildMarkdown_StringSizeAndType_FallsBack()
    {
        using var doc = JsonDocument.Parse("""{"size":"Medium","type":"Beast","cr":"1/4","speed":"40 ft"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Wolf");
        Assert.Contains("*Medium Beast*", md);
        Assert.Contains("**Challenge:** 1/4", md);
    }

    [Fact]
    public void BuildMarkdown_NoAlignment_OmitsComma()
    {
        using var doc = JsonDocument.Parse("""{"size":"Small","type":"Fey"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Pixie");
        Assert.Contains("*Small Fey*", md);
        Assert.DoesNotContain(",", md.Split('\n').First(l => l.Contains('*')));
    }

    [Fact]
    public void BuildMarkdown_EmptyTypeLine_NoItalics()
    {
        using var doc = JsonDocument.Parse("""{"armor_class":"10"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Thing");
        Assert.DoesNotContain("**", md.Split("## Statistics")[0].Replace("# Thing", ""));
    }

    [Fact]
    public void BuildSubtitle_WithCrAndType_IncludesBoth()
    {
        using var doc = JsonDocument.Parse("""{"challenge_rating":"8","type":"monstrosity"}""");
        Assert.Equal("Monster • CR 8 • monstrosity", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_NoCrOrType_JustDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Monster", _sut.BuildSubtitle(doc.RootElement));
    }
}
