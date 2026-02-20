using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class RenderDefinitionHelpersTests
{
    [Theory]
    [InlineData("pk", true)]
    [InlineData("model", true)]
    [InlineData("document__slug", true)]
    [InlineData("name", false)]
    [InlineData("hit_points", false)]
    public void IsHiddenField_ClassifiesCorrectly(string name, bool expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.IsHiddenField(name));
    }

    [Theory]
    [InlineData("pk", true)]   // case-insensitive
    [InlineData("PK", true)]
    [InlineData("Model", true)]
    public void IsHiddenField_IsCaseInsensitive(string name, bool expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.IsHiddenField(name));
    }

    [Fact]
    public void IsNullOrEmpty_NullElement_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("null");
        Assert.True(RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("""{"a":""}""");
        doc.RootElement.TryGetProperty("a", out var val);
        Assert.True(RenderDefinitionHelpers.IsNullOrEmpty(val));
    }

    [Theory]
    [InlineData("\"—\"", true)]
    [InlineData("\"-\"", true)]
    [InlineData("\"  \"", true)]
    [InlineData("\"hello\"", false)]
    public void IsNullOrEmpty_StringVariants(string json, bool expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Fact]
    public void IsNullOrEmpty_EmptyArray_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("[]");
        Assert.True(RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyArray_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("[1]");
        Assert.False(RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Fact]
    public void IsNullOrEmpty_Number_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("42");
        Assert.False(RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Fact]
    public void IsNullOrEmpty_Object_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("{}");
        Assert.False(RenderDefinitionHelpers.IsNullOrEmpty(doc.RootElement));
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("description", true)]
    [InlineData("foo_desc", true)]
    [InlineData("short_description", true)]
    [InlineData("name", false)]
    [InlineData("hit_points", false)]
    public void IsDescriptionField_MatchesCorrectly(string name, bool expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.IsDescriptionField(name));
    }

    [Theory]
    [InlineData("hit_points", "Hit Points")]
    [InlineData("name", "Name")]
    [InlineData("saving_throw_strength", "Saving Throw Strength")]
    public void FormatFieldName_FormatsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.FormatFieldName(input));
    }

    [Theory]
    [InlineData("saving_throw", "Saving Throws")]
    [InlineData("species", "Species")]
    [InlineData("skill_bonus", "Skill Bonus")]  // already ends in 's', no extra
    public void FormatGroupLabel_PluralizesCorrectly(string prefix, string expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.FormatGroupLabel(prefix));
    }

    [Theory]
    [InlineData("saving_throw_fire", "saving_throw", "fire")]
    [InlineData("name", "prefix", "name")]  // no match, returns original
    [InlineData("ability_score_strength", "ability_score", "strength")]
    public void StripPrefix_StripsCorrectly(string name, string prefix, string expected)
    {
        Assert.Equal(expected, RenderDefinitionHelpers.StripPrefix(name, prefix));
    }
}
