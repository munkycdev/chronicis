using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class BlobFilenameParserTests
{
    // ── DeriveSlug ───────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeriveSlug_ReturnsEmpty_ForNullOrWhitespace(string? input)
    {
        Assert.Equal(string.Empty, BlobFilenameParser.DeriveSlug(input!));
    }

    [Fact]
    public void DeriveSlug_RemovesJsonExtension()
    {
        var result = BlobFilenameParser.DeriveSlug("animated-armor.json");

        Assert.Equal("animated-armor", result);
    }

    [Fact]
    public void DeriveSlug_TakesSubstringAfterUnderscore()
    {
        var result = BlobFilenameParser.DeriveSlug("srd-2024_animated-armor.json");

        Assert.Equal("animated-armor", result);
    }

    [Fact]
    public void DeriveSlug_NormalizesToLowercase()
    {
        var result = BlobFilenameParser.DeriveSlug("prefix_Animated-Armor.json");

        Assert.Equal("animated-armor", result);
    }

    [Fact]
    public void DeriveSlug_ReplacesNonAlphanumericWithHyphens()
    {
        var result = BlobFilenameParser.DeriveSlug("prefix_hello world!.json");

        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void DeriveSlug_TrimsLeadingAndTrailingHyphens()
    {
        var result = BlobFilenameParser.DeriveSlug("prefix_--slug--.json");

        Assert.Equal("slug", result);
    }

    [Fact]
    public void DeriveSlug_UsesFullBaseName_WhenNoUnderscore()
    {
        var result = BlobFilenameParser.DeriveSlug("fireball.json");

        Assert.Equal("fireball", result);
    }

    [Fact]
    public void DeriveSlug_FallsBackToBaseName_WhenPostUnderscoreIsEmpty()
    {
        // Underscore at end: "prefix_" → post-underscore is empty → fallback to "prefix"
        var result = BlobFilenameParser.DeriveSlug("prefix_.json");

        Assert.Equal("prefix", result);
    }

    [Fact]
    public void DeriveSlug_HandlesFileWithoutExtension()
    {
        var result = BlobFilenameParser.DeriveSlug("srd-2024_animated-armor");

        Assert.Equal("animated-armor", result);
    }

    // ── PrettifySlug ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PrettifySlug_ReturnsEmpty_ForNullOrWhitespace(string? input)
    {
        Assert.Equal(string.Empty, BlobFilenameParser.PrettifySlug(input!));
    }

    [Fact]
    public void PrettifySlug_TitleCasesHyphenatedSlug()
    {
        Assert.Equal("Animated Armor", BlobFilenameParser.PrettifySlug("animated-armor"));
    }

    [Fact]
    public void PrettifySlug_HandlesSingleWord()
    {
        Assert.Equal("Fireball", BlobFilenameParser.PrettifySlug("fireball"));
    }

    [Fact]
    public void PrettifySlug_HandlesMultipleHyphens()
    {
        Assert.Equal("Red Dragon Wyrmling", BlobFilenameParser.PrettifySlug("red-dragon-wyrmling"));
    }
}
