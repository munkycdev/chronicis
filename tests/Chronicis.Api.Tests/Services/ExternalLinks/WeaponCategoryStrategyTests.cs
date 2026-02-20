using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class WeaponCategoryStrategyTests
{
    private readonly WeaponCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("weapons", _sut.CategoryKey);
        Assert.Equal("weapons", _sut.Endpoint);
        Assert.Equal("Weapon", _sut.DisplayName);
        Assert.Equal("🗡️", _sut.Icon);
        Assert.Equal("weapons", _sut.WebCategory);
    }

    [Fact]
    public void BuildMarkdown_FullWeapon_IncludesAllFields()
    {
        using var doc = JsonDocument.Parse("""
        {"category":"Martial Melee","damage_dice":"1d8","damage_type":"slashing","cost":"15 gp","weight":"3 lb.","properties":["Versatile"]}
        """);
        var md = _sut.BuildMarkdown(doc.RootElement, "Longsword");
        Assert.Contains("*Martial Melee*", md);
        Assert.Contains("**Damage:** 1d8 slashing", md);
        Assert.Contains("**Cost:** 15 gp", md);
        Assert.Contains("**Weight:** 3 lb.", md);
        Assert.Contains("**Properties:** Versatile", md);
    }

    [Fact]
    public void BuildMarkdown_CategoryRangeFallback()
    {
        using var doc = JsonDocument.Parse("""{"category_range":"Simple Melee","damage":"1d4"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Club");
        Assert.Contains("*Simple Melee*", md);
        Assert.Contains("**Damage:** 1d4", md);
    }

    [Fact]
    public void BuildMarkdown_NoDamageType_OmitsSuffix()
    {
        using var doc = JsonDocument.Parse("""{"damage_dice":"1d4"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Club");
        Assert.Contains("**Damage:** 1d4", md);
        Assert.DoesNotContain("**Damage:** 1d4 ", md);
    }

    [Fact]
    public void BuildMarkdown_NoCategory_NoItalics()
    {
        using var doc = JsonDocument.Parse("""{"damage":"1d6"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Stick");
        Assert.DoesNotContain("*", md.Replace("# Stick", "").Replace("**Damage:** 1d6", "").Trim());
    }

    [Fact]
    public void BuildSubtitle_WithCategoryRange()
    {
        using var doc = JsonDocument.Parse("""{"category_range":"Simple Melee"}""");
        Assert.Equal("Weapon • Simple Melee", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_WithCategoryFallback()
    {
        using var doc = JsonDocument.Parse("""{"category":"Martial"}""");
        Assert.Equal("Weapon • Martial", _sut.BuildSubtitle(doc.RootElement));
    }

    [Fact]
    public void BuildSubtitle_NoFields_JustDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Weapon", _sut.BuildSubtitle(doc.RootElement));
    }
}
