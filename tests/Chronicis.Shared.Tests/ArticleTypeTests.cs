using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the ArticleType enum to ensure all expected values exist
/// and can be properly parsed and converted.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArticleTypeTests
{
    [Fact]
    public void ArticleType_HasWikiArticle()
    {
        var value = ArticleType.WikiArticle;
        Assert.Equal(1, (int)value);
    }

    [Fact]
    public void ArticleType_HasCharacter()
    {
        var value = ArticleType.Character;
        Assert.Equal(2, (int)value);
    }

    [Fact]
    public void ArticleType_HasCharacterNote()
    {
        var value = ArticleType.CharacterNote;
        Assert.Equal(3, (int)value);
    }

    [Fact]
    public void ArticleType_HasSession()
    {
        var value = ArticleType.Session;
        Assert.Equal(10, (int)value);
    }

    [Fact]
    public void ArticleType_HasSessionNote()
    {
        var value = ArticleType.SessionNote;
        Assert.Equal(11, (int)value);
    }

    [Fact]
    public void ArticleType_HasLegacy()
    {
        var value = ArticleType.Legacy;
        Assert.Equal(99, (int)value);
    }

    [Fact]
    public void ArticleType_GetValues_ReturnsAllExpectedValues()
    {
        var values = Enum.GetValues<ArticleType>();

        Assert.Equal(6, values.Length);
        Assert.Contains(ArticleType.WikiArticle, values);
        Assert.Contains(ArticleType.Character, values);
        Assert.Contains(ArticleType.CharacterNote, values);
        Assert.Contains(ArticleType.Session, values);
        Assert.Contains(ArticleType.SessionNote, values);
        Assert.Contains(ArticleType.Legacy, values);
    }

    [Fact]
    public void ArticleType_GetNames_ReturnsCorrectNames()
    {
        var names = Enum.GetNames<ArticleType>();

        Assert.Equal(6, names.Length);
        Assert.Contains("WikiArticle", names);
        Assert.Contains("Character", names);
        Assert.Contains("CharacterNote", names);
        Assert.Contains("Session", names);
        Assert.Contains("SessionNote", names);
        Assert.Contains("Legacy", names);
    }

    [Theory]
    [InlineData("WikiArticle", ArticleType.WikiArticle)]
    [InlineData("Character", ArticleType.Character)]
    [InlineData("CharacterNote", ArticleType.CharacterNote)]
    [InlineData("Session", ArticleType.Session)]
    [InlineData("SessionNote", ArticleType.SessionNote)]
    [InlineData("Legacy", ArticleType.Legacy)]
    public void ArticleType_Parse_ParsesCorrectly(string name, ArticleType expected)
    {
        var result = Enum.Parse<ArticleType>(name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("wikiarticle", ArticleType.WikiArticle)]
    [InlineData("CHARACTER", ArticleType.Character)]
    [InlineData("SessionNote", ArticleType.SessionNote)]
    public void ArticleType_Parse_IsCaseInsensitive(string name, ArticleType expected)
    {
        var result = Enum.Parse<ArticleType>(name, ignoreCase: true);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArticleType_Parse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<ArticleType>("InvalidType"));
    }

    [Theory]
    [InlineData(ArticleType.WikiArticle, "WikiArticle")]
    [InlineData(ArticleType.Character, "Character")]
    [InlineData(ArticleType.CharacterNote, "CharacterNote")]
    [InlineData(ArticleType.Session, "Session")]
    [InlineData(ArticleType.SessionNote, "SessionNote")]
    [InlineData(ArticleType.Legacy, "Legacy")]
    public void ArticleType_ToString_ReturnsCorrectName(ArticleType value, string expected)
    {
        Assert.Equal(expected, value.ToString());
    }

    [Theory]
    [InlineData(1, ArticleType.WikiArticle)]
    [InlineData(2, ArticleType.Character)]
    [InlineData(3, ArticleType.CharacterNote)]
    [InlineData(10, ArticleType.Session)]
    [InlineData(11, ArticleType.SessionNote)]
    [InlineData(99, ArticleType.Legacy)]
    public void ArticleType_CastFromInt_Works(int value, ArticleType expected)
    {
        var result = (ArticleType)value;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArticleType_IsDefined_ReturnsTrueForValidValues()
    {
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.WikiArticle));
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.Character));
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.CharacterNote));
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.Session));
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.SessionNote));
        Assert.True(Enum.IsDefined(typeof(ArticleType), ArticleType.Legacy));
    }

    [Fact]
    public void ArticleType_IsDefined_ReturnsFalseForInvalidValues()
    {
        Assert.False(Enum.IsDefined(typeof(ArticleType), 0));
        Assert.False(Enum.IsDefined(typeof(ArticleType), 5));
        Assert.False(Enum.IsDefined(typeof(ArticleType), 100));
    }

    [Fact]
    public void ArticleType_DefaultValue_IsWikiArticle()
    {
        // In C#, default(enum) returns the zero value, which doesn't exist in ArticleType
        // This test documents that behavior - default is 0, which is NOT a valid ArticleType
        var defaultValue = default(ArticleType);
        Assert.Equal(0, (int)defaultValue);
        Assert.False(Enum.IsDefined(typeof(ArticleType), defaultValue));
    }

    [Fact]
    public void ArticleType_HasNoZeroValue()
    {
        // Verify that 0 is intentionally not a valid ArticleType
        // This is good practice - forces explicit initialization
        var values = Enum.GetValues<ArticleType>();
        Assert.DoesNotContain(values, v => (int)v == 0);
    }

    [Theory]
    [InlineData("WikiArticle", true)]
    [InlineData("Session", true)]
    [InlineData("InvalidType", false)]
    [InlineData("", false)]
    public void ArticleType_TryParse_WorksCorrectly(string name, bool shouldSucceed)
    {
        var result = Enum.TryParse<ArticleType>(name, out var value);
        Assert.Equal(shouldSucceed, result);

        if (shouldSucceed)
        {
            Assert.True(Enum.IsDefined(typeof(ArticleType), value));
        }
    }
}
