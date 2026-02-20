using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class ArmorCategoryStrategyTests
{
    private readonly ArmorCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("armor", _sut.CategoryKey);
        Assert.Equal("armor", _sut.Endpoint);
        Assert.Equal("Armor", _sut.DisplayName);
        Assert.Equal("🛡️", _sut.Icon);
        Assert.Equal("armor", _sut.WebCategory);
    }

    [Fact]
    public void BuildMarkdown_FullArmor_IncludesAllFields()
    {
        using var doc = JsonDocument.Parse("""
        {"category":"Heavy Armor","base_ac":"16","cost":"75 gp","weight":"55 lb.","strength_requirement":"13","stealth_disadvantage":"true"}
        """);
        var md = _sut.BuildMarkdown(doc.RootElement, "Chain Mail");
        Assert.Contains("*Heavy Armor*", md);
        Assert.Contains("**Armor Class:** 16", md);
        Assert.Contains("**Cost:** 75 gp", md);
        Assert.Contains("**Weight:** 55 lb.", md);
        Assert.Contains("**Strength Required:** 13", md);
        Assert.Contains("**Stealth:** Disadvantage", md);
    }

    [Fact]
    public void BuildMarkdown_AcStringFallback()
    {
        using var doc = JsonDocument.Parse("""{"ac_string":"12 + Dex (max 2)"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Hide");
        Assert.Contains("**Armor Class:** 12 + Dex (max 2)", md);
    }

    [Fact]
    public void BuildMarkdown_StealthTrueCapitalized_ShowsDisadvantage()
    {
        using var doc = JsonDocument.Parse("""{"stealth_disadvantage":"True"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Plate");
        Assert.Contains("**Stealth:** Disadvantage", md);
    }

    [Fact]
    public void BuildMarkdown_StealthFalse_OmitsDisadvantage()
    {
        using var doc = JsonDocument.Parse("""{"stealth_disadvantage":"false"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Leather");
        Assert.DoesNotContain("Stealth", md);
    }

    [Fact]
    public void BuildMarkdown_NoCategory_NoItalics()
    {
        using var doc = JsonDocument.Parse("""{"base_ac":"11"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Padded");
        Assert.DoesNotContain("*\n", md);
    }

    [Fact]
    public void BuildSubtitle_WithCategory()
    {
        using var doc = JsonDocument.Parse("""{"category":"Heavy Armor"}""");
        Assert.Equal("Armor • Heavy Armor", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_WithCategoryRange()
    {
        using var doc = JsonDocument.Parse("""{"category_range":"Light Armor"}""");
        Assert.Equal("Armor • Light Armor", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_NoFields_JustDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Armor", _sut.BuildSubtitle(doc.RootElement));
    }
}
