using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class PrefixGroupDetectorTests
{
    [Fact]
    public void DetectGroups_FindsGroupWith3PlusFields()
    {
        var fields = MakeFields(
            ("saving_throw_fire", "+1"),
            ("saving_throw_cold", "+2"),
            ("saving_throw_poison", "+3"));

        var groups = PrefixGroupDetector.DetectGroups(fields);

        var group = Assert.Single(groups);
        Assert.Equal("saving_throw", group.Prefix);
        Assert.Equal(3, group.Fields.Count);
    }

    [Fact]
    public void DetectGroups_IgnoresGroupsWithFewerThan3()
    {
        var fields = MakeFields(
            ("saving_throw_fire", "+1"),
            ("saving_throw_cold", "+2"));

        var groups = PrefixGroupDetector.DetectGroups(fields);
        Assert.Empty(groups);
    }

    [Fact]
    public void DetectGroups_IgnoresFieldsWithoutUnderscore()
    {
        var fields = MakeFields(
            ("name", "X"),
            ("level", "5"),
            ("species", "elf"));

        var groups = PrefixGroupDetector.DetectGroups(fields);
        Assert.Empty(groups);
    }

    [Fact]
    public void DetectGroups_LongerPrefixWins_ClaimsFields()
    {
        // Both "ability" and "ability_score" are valid prefixes
        var fields = MakeFields(
            ("ability_score_str", "10"),
            ("ability_score_dex", "12"),
            ("ability_score_con", "14"),
            ("ability_mod_str", "0"),
            ("ability_mod_dex", "1"),
            ("ability_mod_con", "2"));

        var groups = PrefixGroupDetector.DetectGroups(fields);

        // "ability_score" and "ability_mod" are both valid (3+ fields)
        Assert.Equal(2, groups.Count);
        // Longer prefix sorts first
        Assert.Contains(groups, g => g.Prefix == "ability_score");
        Assert.Contains(groups, g => g.Prefix == "ability_mod");
    }

    [Fact]
    public void DetectGroups_FormatsGroupLabel()
    {
        var fields = MakeFields(
            ("skill_bonus_acrobatics", "1"),
            ("skill_bonus_athletics", "2"),
            ("skill_bonus_stealth", "3"));

        var groups = PrefixGroupDetector.DetectGroups(fields);
        Assert.Equal("Skill Bonus", Assert.Single(groups).Label);
    }

    [Fact]
    public void IsAbilityScoreGroup_True_WhenAll6Abilities()
    {
        var group = new PrefixGroup
        {
            Prefix = "ability_score",
            Fields = MakeFields(
                ("ability_score_strength", "10"),
                ("ability_score_dexterity", "12"),
                ("ability_score_constitution", "14"),
                ("ability_score_intelligence", "8"),
                ("ability_score_wisdom", "10"),
                ("ability_score_charisma", "6"))
        };

        Assert.True(PrefixGroupDetector.IsAbilityScoreGroup(group));
    }

    [Fact]
    public void IsAbilityScoreGroup_False_WhenWrongCount()
    {
        var group = new PrefixGroup
        {
            Prefix = "ability_score",
            Fields = MakeFields(
                ("ability_score_strength", "10"),
                ("ability_score_dexterity", "12"))
        };

        Assert.False(PrefixGroupDetector.IsAbilityScoreGroup(group));
    }

    [Fact]
    public void IsAbilityScoreGroup_False_WhenWrongSuffixes()
    {
        var group = new PrefixGroup
        {
            Prefix = "stat",
            Fields = MakeFields(
                ("stat_a", "1"), ("stat_b", "2"), ("stat_c", "3"),
                ("stat_d", "4"), ("stat_e", "5"), ("stat_f", "6"))
        };

        Assert.False(PrefixGroupDetector.IsAbilityScoreGroup(group));
    }

    [Fact]
    public void DetectGroups_FieldWithLeadingUnderscore_Ignored()
    {
        // LastIndexOf('_') == 0 should be skipped
        var fields = MakeFields(
            ("_internal", "x"),
            ("_hidden", "y"),
            ("_secret", "z"));

        var groups = PrefixGroupDetector.DetectGroups(fields);
        Assert.Empty(groups);
    }

    private static List<FieldInfo> MakeFields(params (string name, string value)[] pairs)
    {
        var jsonObj = "{" + string.Join(",", pairs.Select(p => $"\"{p.name}\":\"{p.value}\"")) + "}";
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
}
