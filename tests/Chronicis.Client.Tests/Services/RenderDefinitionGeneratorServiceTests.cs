using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class RenderDefinitionGeneratorServiceTests
{
    [Fact]
    public void Generate_ReturnsMinimal_WhenInputNotObject()
    {
        var element = JsonDocument.Parse("[]").RootElement;

        var result = RenderDefinitionGeneratorService.Generate(element);

        Assert.True(result.CatchAll);
        Assert.Empty(result.Sections);
    }

    [Fact]
    public void Generate_UsesFieldsProperty_WhenPresent()
    {
        var json = """
        {
          "fields": {
            "name": "Goblin",
            "description": "desc",
            "pk": 1
          }
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        Assert.Equal("name", result.TitleField);
        Assert.Contains("pk", result.Hidden);
        Assert.Contains(result.Sections, s => s.Label == "Overview");
    }

    [Fact]
    public void Generate_BuildsAbilityScoreSection_AndAdditionalData()
    {
        var json = """
        {
          "name": "Hero",
          "ability_score_strength": 10,
          "ability_score_dexterity": 11,
          "ability_score_constitution": 12,
          "ability_score_intelligence": 13,
          "ability_score_wisdom": 14,
          "ability_score_charisma": 15,
          "meta": { "x": 1 }
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        var ability = Assert.Single(result.Sections.Where(s => s.Render == "stat-row"));
        Assert.Equal("Ability Scores", ability.Label);
        Assert.Equal(6, ability.Fields!.Count);

        var additional = Assert.Single(result.Sections.Where(s => s.Label == "Additional Data"));
        Assert.True(additional.Collapsed);
    }

    [Fact]
    public void Generate_CreatesGroupedSection_AndCollapsedWhenMostlyNull()
    {
        var json = """
        {
          "name": "Item",
          "saving_throw_fire": "",
          "saving_throw_cold": null,
          "saving_throw_poison": "-",
          "saving_throw_acid": "+1"
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        var group = Assert.Single(result.Sections.Where(s => s.Label == "Saving Throws"));
        Assert.Equal("fields", group.Render);
        Assert.True(group.Collapsed);
    }

    [Fact]
    public void Generate_HandlesDescriptionFieldRendering_AndTitleFallback()
    {
        var json = """
        {
          "foo_desc": "x",
          "hit_points": 8
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        Assert.Equal("name", result.TitleField);
        var overview = Assert.Single(result.Sections.Where(s => s.Label == "Overview"));
        Assert.Contains(overview.Fields!, f => f.Path == "foo_desc" && f.Render == "richtext");
        Assert.Contains(overview.Fields!, f => f.Path == "hit_points" && f.Label == "Hit Points");
    }

    [Fact]
    public void Generate_TreatsEmptyArraysAsNullLikeValues()
    {
        var json = """
        {
          "name": "ArrayCase",
          "saving_throw_fire": [],
          "saving_throw_cold": [],
          "saving_throw_poison": [],
          "saving_throw_acid": "+1"
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);
        var group = Assert.Single(result.Sections.Where(s => s.Label == "Saving Throws"));
        Assert.True(group.Collapsed);
    }

    [Fact]
    public void Generate_HandlesNoUnderscoreAndPluralizationBranches()
    {
        var json = """
        {
          "name": "X",
          "species": "elf",
          "ability": "none",
          "saving_throw_strength": "+1",
          "saving_throw_dexterity": "+1",
          "saving_throw_constitution": "+1"
        }
        """;

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        Assert.Contains(result.Sections, s => s.Label == "Saving Throws");
        Assert.DoesNotContain(result.Sections, s => s.Label == "Speciess");
    }

    [Fact]
    public void Generate_UsesTitle_WhenNameNotPresent()
    {
        var json = """{"title":"My Article","level":5}""";

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        Assert.Equal("title", result.TitleField);
    }

    [Fact]
    public void Generate_NoSections_WhenOnlyTitleAndHidden()
    {
        var json = """{"name":"X","pk":1,"model":"m"}""";

        var result = RenderDefinitionGeneratorService.Generate(JsonDocument.Parse(json).RootElement);

        Assert.Empty(result.Sections);
        Assert.Contains("pk", result.Hidden);
        Assert.Contains("model", result.Hidden);
    }
}
