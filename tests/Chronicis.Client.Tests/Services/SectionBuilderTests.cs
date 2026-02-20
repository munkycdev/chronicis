using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class SectionBuilderTests
{
    [Fact]
    public void Build_CreatesOverviewSection_ForSimpleFields()
    {
        var remaining = MakeFields(("hit_points", "7"), ("armor_class", "12"));
        var sections = SectionBuilder.Build(remaining, new List<PrefixGroup>());

        var overview = Assert.Single(sections);
        Assert.Equal("Overview", overview.Label);
        Assert.Equal("fields", overview.Render);
        Assert.Equal(2, overview.Fields!.Count);
    }

    [Fact]
    public void Build_DescriptionFields_GetRichtextRender()
    {
        var remaining = MakeFields(("desc", "A creature."), ("hit_points", "10"));
        var sections = SectionBuilder.Build(remaining, new List<PrefixGroup>());

        var overview = sections.First(s => s.Label == "Overview");
        Assert.Contains(overview.Fields!, f => f.Path == "desc" && f.Render == "richtext");
        Assert.Contains(overview.Fields!, f => f.Path == "hit_points" && f.Render == "text");
    }

    [Fact]
    public void Build_DescriptionFields_SortedAfterOtherFields()
    {
        var remaining = MakeFields(("desc", "First"), ("hit_points", "10"));
        var sections = SectionBuilder.Build(remaining, new List<PrefixGroup>());

        var overview = sections.First(s => s.Label == "Overview");
        // hit_points should come before desc due to sort order
        Assert.Equal("hit_points", overview.Fields![0].Path);
        Assert.Equal("desc", overview.Fields![1].Path);
    }

    [Fact]
    public void Build_CreatesAbilityScoreSection_WithStatRowRender()
    {
        var fields = MakeFields(
            ("ability_score_strength", "10"), ("ability_score_dexterity", "12"),
            ("ability_score_constitution", "14"), ("ability_score_intelligence", "8"),
            ("ability_score_wisdom", "10"), ("ability_score_charisma", "6"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);

        var ability = Assert.Single(sections.Where(s => s.Render == "stat-row"));
        Assert.Equal("Ability Scores", ability.Label);
        Assert.Equal(6, ability.Fields!.Count);
        Assert.Equal("STR", ability.Fields[0].Label);
        Assert.Equal("CHA", ability.Fields[5].Label);
    }

    [Fact]
    public void Build_GroupedSection_CollapsedWhenMostlyNull()
    {
        var fields = MakeFields(
            ("saving_throw_fire", ""), ("saving_throw_cold", ""),
            ("saving_throw_poison", "-"), ("saving_throw_acid", "+1"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);

        var group = Assert.Single(sections.Where(s => s.Label == "Saving Throws"));
        Assert.True(group.Collapsed);
    }

    [Fact]
    public void Build_GroupedSection_NotCollapsedWhenMostlyPopulated()
    {
        var fields = MakeFields(
            ("saving_throw_fire", "+1"), ("saving_throw_cold", "+2"),
            ("saving_throw_poison", "+3"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);

        var group = Assert.Single(sections.Where(s => s.Label == "Saving Throws"));
        Assert.False(group.Collapsed);
    }

    [Fact]
    public void Build_GroupedSection_StripsPrefix_InFieldLabels()
    {
        var fields = MakeFields(
            ("saving_throw_fire", "+1"), ("saving_throw_cold", "+2"),
            ("saving_throw_poison", "+3"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);
        var group = sections.First(s => s.Label == "Saving Throws");
        Assert.Contains(group.Fields!, f => f.Label == "Fire");
        Assert.Contains(group.Fields!, f => f.Label == "Cold");
    }

    [Fact]
    public void Build_ComplexFields_GoToAdditionalDataSection()
    {
        var remaining = MakeComplexFields(("meta", true), ("items", true), ("simple", false));
        var sections = SectionBuilder.Build(remaining, new List<PrefixGroup>());

        var additional = Assert.Single(sections.Where(s => s.Label == "Additional Data"));
        Assert.True(additional.Collapsed);
        Assert.Equal(2, additional.Fields!.Count);
    }

    [Fact]
    public void Build_NoFields_ReturnsEmptySections()
    {
        var sections = SectionBuilder.Build(new List<FieldInfo>(), new List<PrefixGroup>());
        Assert.Empty(sections);
    }

    [Fact]
    public void Build_GroupedFieldsExcludedFromOverview()
    {
        var fields = MakeFields(
            ("hit_points", "10"),
            ("saving_throw_fire", "+1"), ("saving_throw_cold", "+2"),
            ("saving_throw_poison", "+3"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);

        var overview = sections.FirstOrDefault(s => s.Label == "Overview");
        Assert.NotNull(overview);
        Assert.Single(overview!.Fields!); // only hit_points
        Assert.Equal("hit_points", overview.Fields![0].Path);
    }

    [Fact]
    public void Build_NullFieldsSortedAfterPopulated_InGroupedSection()
    {
        var fields = MakeFields(
            ("saving_throw_fire", ""), ("saving_throw_cold", "+2"),
            ("saving_throw_poison", "+3"));
        var groups = PrefixGroupDetector.DetectGroups(fields);

        var sections = SectionBuilder.Build(fields, groups);
        var group = sections.First(s => s.Label == "Saving Throws");
        // Non-null fields should come first
        Assert.False(RenderDefinitionHelpers.IsNullOrEmpty(
            JsonDocument.Parse($"\"{group.Fields![0].Path}\"").RootElement) || group.Fields[0].Path == "saving_throw_fire");
    }

    private static List<FieldInfo> MakeFields(params (string name, string value)[] pairs)
    {
        var jsonObj = "{" + string.Join(",", pairs.Select(p =>
            p.value == "" ? $"\"{p.name}\":\"\"" :
            p.value == "-" ? $"\"{p.name}\":\"-\"" :
            $"\"{p.name}\":\"{p.value}\"")) + "}";
        var doc = JsonDocument.Parse(jsonObj);
        return doc.RootElement.EnumerateObject().Select(p => new FieldInfo
        {
            Name = p.Name,
            Value = p.Value,
            Kind = p.Value.ValueKind,
            IsNull = RenderDefinitionHelpers.IsNullOrEmpty(p.Value),
            IsComplex = p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
        }).ToList();
    }

    private static List<FieldInfo> MakeComplexFields(params (string name, bool isComplex)[] pairs)
    {
        var parts = pairs.Select(p => p.isComplex
            ? $"\"{p.name}\":{{\"x\":1}}"
            : $"\"{p.name}\":\"text\"");
        var jsonObj = "{" + string.Join(",", parts) + "}";
        var doc = JsonDocument.Parse(jsonObj);
        return doc.RootElement.EnumerateObject().Select(p => new FieldInfo
        {
            Name = p.Name,
            Value = p.Value,
            Kind = p.Value.ValueKind,
            IsNull = false,
            IsComplex = p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
        }).ToList();
    }
}
