using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class SrdJsonHelperTests
{
    // ── ExtractTitle ─────────────────────────────────────────────

    [Fact]
    public void ExtractTitle_ReturnsFieldsName_WhenPresent()
    {
        using var doc = JsonDocument.Parse("""{"fields":{"name":"Fireball"}}""");

        Assert.Equal("Fireball", SrdJsonHelper.ExtractTitle(doc, "fallback"));
    }

    [Fact]
    public void ExtractTitle_ReturnsPrettifiedSlug_WhenNameMissing()
    {
        using var doc = JsonDocument.Parse("""{"fields":{"level":3}}""");

        Assert.Equal("Animated Armor", SrdJsonHelper.ExtractTitle(doc, "animated-armor"));
    }

    [Fact]
    public void ExtractTitle_ReturnsFallback_WhenFieldsObjectMissing()
    {
        using var doc = JsonDocument.Parse("""{"pk":"spells.fireball"}""");

        Assert.Equal("Fireball", SrdJsonHelper.ExtractTitle(doc, "fireball"));
    }

    [Fact]
    public void ExtractTitle_ReturnsFallback_WhenNameIsWhitespace()
    {
        using var doc = JsonDocument.Parse("""{"fields":{"name":"   "}}""");

        Assert.Equal("Fallback", SrdJsonHelper.ExtractTitle(doc, "fallback"));
    }

    [Fact]
    public void ExtractTitle_WhenFieldsIsNotObject_FallsBackWithoutThrowing()
    {
        using var doc = JsonDocument.Parse("""{"fields":123}""");

        Assert.Equal("Fallback", SrdJsonHelper.ExtractTitle(doc, "fallback"));
    }

    // ── ExtractPk ────────────────────────────────────────────────

    [Fact]
    public void ExtractPk_ReturnsPk_WhenPresent()
    {
        using var doc = JsonDocument.Parse("""{"pk":"spells.fireball","fields":{}}""");

        Assert.Equal("spells.fireball", SrdJsonHelper.ExtractPk(doc));
    }

    [Fact]
    public void ExtractPk_ReturnsNull_WhenMissing()
    {
        using var doc = JsonDocument.Parse("""{"fields":{"name":"Fireball"}}""");

        Assert.Null(SrdJsonHelper.ExtractPk(doc));
    }

    [Fact]
    public void ExtractPk_ReturnsNull_WhenPkIsNotString()
    {
        using var doc = JsonDocument.Parse("""{"pk":42}""");

        Assert.Null(SrdJsonHelper.ExtractPk(doc));
    }

    [Fact]
    public void ExtractPk_WhenRootIsNotObject_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");

        Assert.Null(SrdJsonHelper.ExtractPk(doc));
    }
}
