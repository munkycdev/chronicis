namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the SlugGenerator utility class.
/// Validates URL-safe slug generation, validation, and uniqueness handling.
/// </summary>
public class SlugGeneratorTests
{
    // ────────────────────────────────────────────────────────────────
    //  GenerateSlug - Normal Cases
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSlug_SimpleTitle_ReturnsLowercase()
    {
        var result = SlugGenerator.GenerateSlug("Hello World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_SingleWord_ReturnsLowercase()
    {
        var result = SlugGenerator.GenerateSlug("Article");
        Assert.Equal("article", result);
    }

    [Fact]
    public void GenerateSlug_WithNumbers_PreservesNumbers()
    {
        var result = SlugGenerator.GenerateSlug("Chapter 42");
        Assert.Equal("chapter-42", result);
    }

    [Fact]
    public void GenerateSlug_MixedCase_ConvertsToLowercase()
    {
        var result = SlugGenerator.GenerateSlug("CamelCaseTitle");
        Assert.Equal("camelcasetitle", result);
    }

    [Fact]
    public void GenerateSlug_AlreadyValidSlug_ReturnsUnchanged()
    {
        var result = SlugGenerator.GenerateSlug("already-valid-slug");
        Assert.Equal("already-valid-slug", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateSlug - Special Characters
    // ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Hello@World!", "helloworld")]
    [InlineData("Test#123$456", "test123456")]
    [InlineData("Question?Answer", "questionanswer")]
    [InlineData("Name & Title", "name-title")]
    [InlineData("C++ Programming", "c-programming")]
    [InlineData("100% Complete", "100-complete")]
    public void GenerateSlug_SpecialCharacters_RemovesThemCorrectly(string input, string expected)
    {
        var result = SlugGenerator.GenerateSlug(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateSlug_Punctuation_RemovesPunctuation()
    {
        var result = SlugGenerator.GenerateSlug("Hello, World!");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_Apostrophes_RemovesApostrophes()
    {
        var result = SlugGenerator.GenerateSlug("It's a Test");
        Assert.Equal("its-a-test", result);
    }

    [Fact]
    public void GenerateSlug_Parentheses_RemovesParentheses()
    {
        var result = SlugGenerator.GenerateSlug("Title (Subtitle)");
        Assert.Equal("title-subtitle", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateSlug - Whitespace Handling
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSlug_MultipleSpaces_CollapsesToSingleHyphen()
    {
        var result = SlugGenerator.GenerateSlug("Hello    World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_LeadingSpaces_TrimsLeadingHyphens()
    {
        var result = SlugGenerator.GenerateSlug("   Hello World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_TrailingSpaces_TrimsTrailingHyphens()
    {
        var result = SlugGenerator.GenerateSlug("Hello World   ");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_LeadingAndTrailingSpaces_TrimsBothSides()
    {
        var result = SlugGenerator.GenerateSlug("   Hello World   ");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_TabsAndNewlines_RemovesThem()
    {
        var result = SlugGenerator.GenerateSlug("Hello\tWorld\nTest");
        Assert.Equal("helloworldtest", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateSlug - Hyphen Handling
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSlug_MultipleConsecutiveHyphens_CollapsesToSingle()
    {
        var result = SlugGenerator.GenerateSlug("Hello--World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_ManyConsecutiveHyphens_CollapsesToSingle()
    {
        var result = SlugGenerator.GenerateSlug("Hello-----World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_LeadingHyphens_TrimsLeadingHyphens()
    {
        var result = SlugGenerator.GenerateSlug("---hello-world");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_TrailingHyphens_TrimsTrailingHyphens()
    {
        var result = SlugGenerator.GenerateSlug("hello-world---");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_LeadingAndTrailingHyphens_TrimsBothSides()
    {
        var result = SlugGenerator.GenerateSlug("---hello-world---");
        Assert.Equal("hello-world", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateSlug - Edge Cases
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSlug_NullInput_ReturnsUntitled()
    {
        var result = SlugGenerator.GenerateSlug(null!);
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GenerateSlug_EmptyString_ReturnsUntitled()
    {
        var result = SlugGenerator.GenerateSlug("");
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GenerateSlug_WhitespaceOnly_ReturnsUntitled()
    {
        var result = SlugGenerator.GenerateSlug("   ");
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GenerateSlug_OnlySpecialCharacters_ReturnsUntitled()
    {
        var result = SlugGenerator.GenerateSlug("!@#$%^&*()");
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GenerateSlug_OnlyHyphens_ReturnsUntitled()
    {
        var result = SlugGenerator.GenerateSlug("-----");
        Assert.Equal("untitled", result);
    }

    [Fact]
    public void GenerateSlug_UnicodeCharacters_RemovesUnicode()
    {
        var result = SlugGenerator.GenerateSlug("Café");
        Assert.Equal("caf", result);
    }

    [Fact]
    public void GenerateSlug_Emojis_RemovesEmojis()
    {
        var result = SlugGenerator.GenerateSlug("Hello 😀 World");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void GenerateSlug_ChineseCharacters_RemovesNonAscii()
    {
        var result = SlugGenerator.GenerateSlug("你好世界");
        Assert.Equal("untitled", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  IsValidSlug
    // ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("hello-world", true)]
    [InlineData("test123", true)]
    [InlineData("a", true)]
    [InlineData("simple", true)]
    [InlineData("multi-word-slug", true)]
    [InlineData("with-numbers-123", true)]
    [InlineData("-leading", true)] // Actually valid per the regex
    [InlineData("trailing-", true)] // Actually valid per the regex
    [InlineData("multiple--hyphens", true)] // Actually valid per the regex
    public void IsValidSlug_ValidSlugs_ReturnsTrue(string slug, bool expected)
    {
        var result = SlugGenerator.IsValidSlug(slug);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", false)]
    [InlineData("test_case", false)]
    [InlineData("hello!", false)]
    [InlineData("UPPERCASE", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public void IsValidSlug_InvalidSlugs_ReturnsFalse(string? slug, bool expected)
    {
        var result = SlugGenerator.IsValidSlug(slug);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidSlug_SpecialCharacters_ReturnsFalse()
    {
        Assert.False(SlugGenerator.IsValidSlug("hello@world"));
        Assert.False(SlugGenerator.IsValidSlug("test#123"));
        Assert.False(SlugGenerator.IsValidSlug("question?"));
    }

    [Fact]
    public void IsValidSlug_Underscore_ReturnsFalse()
    {
        // Underscores are not allowed in slugs
        Assert.False(SlugGenerator.IsValidSlug("hello_world"));
    }

    [Fact]
    public void IsValidSlug_MixedCase_ReturnsFalse()
    {
        Assert.False(SlugGenerator.IsValidSlug("HelloWorld"));
        Assert.False(SlugGenerator.IsValidSlug("Hello-World"));
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateUniqueSlug
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateUniqueSlug_NoCollision_ReturnsBaseSlug()
    {
        var existingSlugs = new HashSet<string> { "other-slug", "different-slug" };
        var result = SlugGenerator.GenerateUniqueSlug("new-slug", existingSlugs);
        Assert.Equal("new-slug", result);
    }

    [Fact]
    public void GenerateUniqueSlug_EmptySet_ReturnsBaseSlug()
    {
        var existingSlugs = new HashSet<string>();
        var result = SlugGenerator.GenerateUniqueSlug("my-slug", existingSlugs);
        Assert.Equal("my-slug", result);
    }

    [Fact]
    public void GenerateUniqueSlug_SingleCollision_AppendsDash2()
    {
        var existingSlugs = new HashSet<string> { "my-slug" };
        var result = SlugGenerator.GenerateUniqueSlug("my-slug", existingSlugs);
        Assert.Equal("my-slug-2", result);
    }

    [Fact]
    public void GenerateUniqueSlug_MultipleCollisions_IncrementsSuffix()
    {
        var existingSlugs = new HashSet<string> { "my-slug", "my-slug-2", "my-slug-3" };
        var result = SlugGenerator.GenerateUniqueSlug("my-slug", existingSlugs);
        Assert.Equal("my-slug-4", result);
    }

    [Fact]
    public void GenerateUniqueSlug_NonConsecutiveCollisions_FindsFirstGap()
    {
        var existingSlugs = new HashSet<string> { "my-slug", "my-slug-2" };
        var result = SlugGenerator.GenerateUniqueSlug("my-slug", existingSlugs);
        Assert.Equal("my-slug-3", result);
    }

    [Fact]
    public void GenerateUniqueSlug_LargeNumberOfCollisions_HandlesCorrectly()
    {
        var existingSlugs = new HashSet<string>();
        for (int i = 1; i <= 100; i++)
        {
            if (i == 1)
                existingSlugs.Add("slug");
            else
                existingSlugs.Add($"slug-{i}");
        }

        var result = SlugGenerator.GenerateUniqueSlug("slug", existingSlugs);
        Assert.Equal("slug-101", result);
    }

    [Fact]
    public void GenerateUniqueSlug_MixedCollisions_HandlesCorrectly()
    {
        var existingSlugs = new HashSet<string>
        {
            "test-slug",
            "other-slug",
            "test-slug-2",
            "another-slug",
            "test-slug-3"
        };

        var result = SlugGenerator.GenerateUniqueSlug("test-slug", existingSlugs);
        Assert.Equal("test-slug-4", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Integration Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Integration_GenerateAndValidate_WorkTogether()
    {
        var title = "Hello World";
        var slug = SlugGenerator.GenerateSlug(title);

        Assert.True(SlugGenerator.IsValidSlug(slug));
    }

    [Theory]
    [InlineData("Simple Title")]
    [InlineData("Complex! Title@ With# Special$")]
    [InlineData("   Spaces   Everywhere   ")]
    [InlineData("Numbers 123 456")]
    [InlineData("hyphen-separated-words")]
    public void Integration_GeneratedSlugsAreAlwaysValid(string title)
    {
        var slug = SlugGenerator.GenerateSlug(title);
        Assert.True(SlugGenerator.IsValidSlug(slug));
    }

    [Fact]
    public void Integration_GenerateUnique_ProducesValidSlug()
    {
        var existingSlugs = new HashSet<string> { "test" };
        var slug = SlugGenerator.GenerateUniqueSlug("test", existingSlugs);

        Assert.True(SlugGenerator.IsValidSlug(slug));
        Assert.Equal("test-2", slug);
    }

    [Fact]
    public void Integration_FullWorkflow_TitleToUniqueSlug()
    {
        var title = "My Article Title";
        var existingSlugs = new HashSet<string> { "my-article-title" };

        var baseSlug = SlugGenerator.GenerateSlug(title);
        var uniqueSlug = SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);

        Assert.Equal("my-article-title", baseSlug);
        Assert.Equal("my-article-title-2", uniqueSlug);
        Assert.True(SlugGenerator.IsValidSlug(uniqueSlug));
    }
}
