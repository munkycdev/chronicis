using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class RaceCategoryStrategyTests
{
    private readonly RaceCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("races", _sut.CategoryKey);
        Assert.Equal("races", _sut.Endpoint);
        Assert.Equal("Race", _sut.DisplayName);
        Assert.Equal("👤", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_FullRace_IncludesAllSections()
    {
        using var doc = JsonDocument.Parse("""
        {"size":"Medium","speed":"30 ft","desc":"Elves are a magical people.","traits":"Darkvision, Keen Senses"}
        """);
        var md = _sut.BuildMarkdown(doc.RootElement, "Elf");
        Assert.Contains("# Elf", md);
        Assert.Contains("**Size:** Medium", md);
        Assert.Contains("**Speed:** 30 ft", md);
        Assert.Contains("Elves are a magical people.", md);
        Assert.Contains("## Traits", md);
        Assert.Contains("Darkvision, Keen Senses", md);
    }

    [Fact]
    public void BuildMarkdown_NoSizeOrSpeed_OmitsFields()
    {
        using var doc = JsonDocument.Parse("""{"desc":"Custom race."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.DoesNotContain("Size", md);
        Assert.DoesNotContain("Speed", md);
    }

    [Fact]
    public void BuildMarkdown_NoTraits_OmitsSection()
    {
        using var doc = JsonDocument.Parse("""{"desc":"Simple."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.DoesNotContain("## Traits", md);
    }

    [Fact]
    public void BuildMarkdown_NoDesc_JustTitle()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Custom");
        Assert.Contains("# Custom", md);
    }

    [Fact]
    public void BuildSubtitle_ReturnsDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Race", _sut.BuildSubtitle(doc.RootElement));
    }
}
