using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the ArticleVisibility enum to ensure all expected values exist
/// and can be properly parsed and converted.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArticleVisibilityTests
{
    [Fact]
    public void ArticleVisibility_HasPublic()
    {
        var value = ArticleVisibility.Public;
        Assert.Equal(0, (int)value);
    }

    [Fact]
    public void ArticleVisibility_HasMembersOnly()
    {
        var value = ArticleVisibility.MembersOnly;
        Assert.Equal(1, (int)value);
    }

    [Fact]
    public void ArticleVisibility_HasPrivate()
    {
        var value = ArticleVisibility.Private;
        Assert.Equal(2, (int)value);
    }

    [Fact]
    public void ArticleVisibility_GetValues_ReturnsAllExpectedValues()
    {
        var values = Enum.GetValues<ArticleVisibility>();

        Assert.Equal(3, values.Length);
        Assert.Contains(ArticleVisibility.Public, values);
        Assert.Contains(ArticleVisibility.MembersOnly, values);
        Assert.Contains(ArticleVisibility.Private, values);
    }

    [Fact]
    public void ArticleVisibility_GetNames_ReturnsCorrectNames()
    {
        var names = Enum.GetNames<ArticleVisibility>();

        Assert.Equal(3, names.Length);
        Assert.Contains("Public", names);
        Assert.Contains("MembersOnly", names);
        Assert.Contains("Private", names);
    }

    [Theory]
    [InlineData("Public", ArticleVisibility.Public)]
    [InlineData("MembersOnly", ArticleVisibility.MembersOnly)]
    [InlineData("Private", ArticleVisibility.Private)]
    public void ArticleVisibility_Parse_ParsesCorrectly(string name, ArticleVisibility expected)
    {
        var result = Enum.Parse<ArticleVisibility>(name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("public", ArticleVisibility.Public)]
    [InlineData("MEMBERSONLY", ArticleVisibility.MembersOnly)]
    [InlineData("Private", ArticleVisibility.Private)]
    public void ArticleVisibility_Parse_IsCaseInsensitive(string name, ArticleVisibility expected)
    {
        var result = Enum.Parse<ArticleVisibility>(name, ignoreCase: true);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArticleVisibility_Parse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<ArticleVisibility>("InvalidVisibility"));
    }

    [Theory]
    [InlineData(ArticleVisibility.Public, "Public")]
    [InlineData(ArticleVisibility.MembersOnly, "MembersOnly")]
    [InlineData(ArticleVisibility.Private, "Private")]
    public void ArticleVisibility_ToString_ReturnsCorrectName(ArticleVisibility value, string expected)
    {
        Assert.Equal(expected, value.ToString());
    }

    [Theory]
    [InlineData(0, ArticleVisibility.Public)]
    [InlineData(1, ArticleVisibility.MembersOnly)]
    [InlineData(2, ArticleVisibility.Private)]
    public void ArticleVisibility_CastFromInt_Works(int value, ArticleVisibility expected)
    {
        var result = (ArticleVisibility)value;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArticleVisibility_IsDefined_ReturnsTrueForValidValues()
    {
        Assert.True(Enum.IsDefined(typeof(ArticleVisibility), ArticleVisibility.Public));
        Assert.True(Enum.IsDefined(typeof(ArticleVisibility), ArticleVisibility.MembersOnly));
        Assert.True(Enum.IsDefined(typeof(ArticleVisibility), ArticleVisibility.Private));
    }

    [Fact]
    public void ArticleVisibility_IsDefined_ReturnsFalseForInvalidValues()
    {
        Assert.False(Enum.IsDefined(typeof(ArticleVisibility), 3));
        Assert.False(Enum.IsDefined(typeof(ArticleVisibility), -1));
        Assert.False(Enum.IsDefined(typeof(ArticleVisibility), 99));
    }

    [Fact]
    public void ArticleVisibility_DefaultValue_IsPublic()
    {
        var defaultValue = default(ArticleVisibility);
        Assert.Equal(ArticleVisibility.Public, defaultValue);
        Assert.Equal(0, (int)defaultValue);
    }

    [Theory]
    [InlineData("Public", true)]
    [InlineData("MembersOnly", true)]
    [InlineData("Private", true)]
    [InlineData("InvalidVisibility", false)]
    [InlineData("", false)]
    public void ArticleVisibility_TryParse_WorksCorrectly(string name, bool shouldSucceed)
    {
        var result = Enum.TryParse<ArticleVisibility>(name, out var value);
        Assert.Equal(shouldSucceed, result);

        if (shouldSucceed)
        {
            Assert.True(Enum.IsDefined(typeof(ArticleVisibility), value));
        }
    }

    [Fact]
    public void ArticleVisibility_IsOrderedByRestrictionLevel()
    {
        // Verify that enum values are ordered from least to most restrictive
        // as documented in the enum comments
        Assert.True((int)ArticleVisibility.Public < (int)ArticleVisibility.MembersOnly);
        Assert.True((int)ArticleVisibility.MembersOnly < (int)ArticleVisibility.Private);
    }

    [Theory]
    [InlineData(ArticleVisibility.Public, ArticleVisibility.MembersOnly, true)]
    [InlineData(ArticleVisibility.Public, ArticleVisibility.Private, true)]
    [InlineData(ArticleVisibility.MembersOnly, ArticleVisibility.Private, true)]
    [InlineData(ArticleVisibility.MembersOnly, ArticleVisibility.Public, false)]
    [InlineData(ArticleVisibility.Private, ArticleVisibility.Public, false)]
    public void ArticleVisibility_Comparison_ReflectsRestrictionLevel(
        ArticleVisibility less,
        ArticleVisibility more,
        bool lessIsLess)
    {
        Assert.Equal(lessIsLess, (int)less < (int)more);
    }

    [Fact]
    public void ArticleVisibility_AllValuesCovered()
    {
        // Ensure no gaps in the sequence 0, 1, 2
        var values = Enum.GetValues<ArticleVisibility>().Select(v => (int)v).OrderBy(v => v).ToList();

        Assert.Equal(0, values[0]);
        Assert.Equal(1, values[1]);
        Assert.Equal(2, values[2]);

        // No gaps
        for (int i = 0; i < values.Count - 1; i++)
        {
            Assert.Equal(1, values[i + 1] - values[i]);
        }
    }
}
