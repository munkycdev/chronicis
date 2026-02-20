using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class ConditionCategoryStrategyTests
{
    private readonly ConditionCategoryStrategy _sut = new();

    [Fact]
    public void Properties_AreCorrect()
    {
        Assert.Equal("conditions", _sut.CategoryKey);
        Assert.Equal("conditions", _sut.Endpoint);
        Assert.Equal("Condition", _sut.DisplayName);
        Assert.Equal("⚡", _sut.Icon);
    }

    [Fact]
    public void BuildMarkdown_WithDesc_IncludesDescription()
    {
        using var doc = JsonDocument.Parse("""{"desc":"A blinded creature can't see."}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Blinded");
        Assert.Contains("# Blinded", md);
        Assert.Contains("A blinded creature can't see.", md);
    }

    [Fact]
    public void BuildMarkdown_NoDesc_JustTitle()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        var md = _sut.BuildMarkdown(doc.RootElement, "Unknown");
        Assert.Contains("# Unknown", md);
    }

    [Fact]
    public void BuildSubtitle_ReturnsDisplayName()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Condition", _sut.BuildSubtitle(doc.RootElement));
    }
}
