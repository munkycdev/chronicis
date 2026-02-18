using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class BlobIdValidatorTests
{
    // ── IsValid ──────────────────────────────────────────────────

    [Theory]
    [InlineData("spells/fireball")]
    [InlineData("items/armor/breastplate")]
    [InlineData("bestiary/Beast/aboar")]
    public void IsValid_AcceptsValidIds(string id)
    {
        Assert.True(BlobIdValidator.IsValid(id, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_RejectsNullOrEmpty(string? id)
    {
        Assert.False(BlobIdValidator.IsValid(id!, out var error));
        Assert.Contains("empty", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("spells/../etc/passwd")]
    [InlineData("items/..hidden")]
    public void IsValid_RejectsPathTraversal(string id)
    {
        Assert.False(BlobIdValidator.IsValid(id, out var error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("spells/fire.ball")]
    [InlineData("items\\armor")]
    [InlineData("spells/[fireball]")]
    [InlineData("spells/fire|ball")]
    public void IsValid_RejectsProhibitedChars(string id)
    {
        Assert.False(BlobIdValidator.IsValid(id, out var error));
        Assert.Contains("prohibited", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fireball")]           // no slash
    [InlineData("/fireball")]          // starts with slash
    [InlineData("spells/")]            // ends with slash
    [InlineData("spells/fire ball")]   // space
    public void IsValid_RejectsInvalidFormat(string id)
    {
        Assert.False(BlobIdValidator.IsValid(id, out _));
    }

    // ── ParseId ──────────────────────────────────────────────────

    [Fact]
    public void ParseId_SplitsTwoPartId()
    {
        var (category, slug) = BlobIdValidator.ParseId("spells/fireball");

        Assert.Equal("spells", category);
        Assert.Equal("fireball", slug);
    }

    [Fact]
    public void ParseId_SplitsThreePartId()
    {
        var (category, slug) = BlobIdValidator.ParseId("items/armor/breastplate");

        Assert.Equal("items/armor", category);
        Assert.Equal("breastplate", slug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("noslash")]
    [InlineData("/leading")]
    [InlineData("trailing/")]
    public void ParseId_ReturnsNulls_ForInvalidInput(string? id)
    {
        var (category, slug) = BlobIdValidator.ParseId(id!);

        Assert.Null(category);
        Assert.Null(slug);
    }
}
