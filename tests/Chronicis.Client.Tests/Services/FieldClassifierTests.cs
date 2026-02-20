using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class FieldClassifierTests
{
    [Fact]
    public void Classify_FindsNameAsTitleField()
    {
        using var doc = JsonDocument.Parse("""{"name":"Goblin","hit_points":7}""");
        var (titleField, _, _) = FieldClassifier.Classify(doc.RootElement);
        Assert.Equal("name", titleField);
    }

    [Fact]
    public void Classify_FindsTitleAsTitleField_WhenNameMissing()
    {
        using var doc = JsonDocument.Parse("""{"title":"My Article","level":5}""");
        var (titleField, _, _) = FieldClassifier.Classify(doc.RootElement);
        Assert.Equal("title", titleField);
    }

    [Fact]
    public void Classify_DefaultsToName_WhenNeitherPresent()
    {
        using var doc = JsonDocument.Parse("""{"hit_points":7,"armor_class":12}""");
        var (titleField, _, _) = FieldClassifier.Classify(doc.RootElement);
        Assert.Equal("name", titleField);
    }

    [Fact]
    public void Classify_SeparatesHiddenFields()
    {
        using var doc = JsonDocument.Parse("""{"name":"X","pk":1,"model":"m","hit_points":7}""");
        var (_, hidden, remaining) = FieldClassifier.Classify(doc.RootElement);

        Assert.Contains("pk", hidden);
        Assert.Contains("model", hidden);
        Assert.DoesNotContain(hidden, h => h == "name");
        Assert.DoesNotContain(hidden, h => h == "hit_points");
        Assert.Single(remaining); // just hit_points
        Assert.Equal("hit_points", remaining[0].Name);
    }

    [Fact]
    public void Classify_ExcludesTitleFieldFromRemaining()
    {
        using var doc = JsonDocument.Parse("""{"name":"Goblin","level":3}""");
        var (_, _, remaining) = FieldClassifier.Classify(doc.RootElement);

        Assert.DoesNotContain(remaining, f => f.Name == "name");
        Assert.Single(remaining);
    }

    [Fact]
    public void Classify_DetectsNullFields()
    {
        using var doc = JsonDocument.Parse("""{"name":"X","empty":"","dash":"-","ok":"value"}""");
        var (_, _, remaining) = FieldClassifier.Classify(doc.RootElement);

        var empty = remaining.First(f => f.Name == "empty");
        var dash = remaining.First(f => f.Name == "dash");
        var ok = remaining.First(f => f.Name == "ok");

        Assert.True(empty.IsNull);
        Assert.True(dash.IsNull);
        Assert.False(ok.IsNull);
    }

    [Fact]
    public void Classify_DetectsComplexFields()
    {
        using var doc = JsonDocument.Parse("""{"name":"X","meta":{"a":1},"items":[1,2],"simple":"text"}""");
        var (_, _, remaining) = FieldClassifier.Classify(doc.RootElement);

        Assert.True(remaining.First(f => f.Name == "meta").IsComplex);
        Assert.True(remaining.First(f => f.Name == "items").IsComplex);
        Assert.False(remaining.First(f => f.Name == "simple").IsComplex);
    }

    [Fact]
    public void Classify_CaseInsensitiveHiddenMatch()
    {
        using var doc = JsonDocument.Parse("""{"name":"X","PK":1,"Document__Slug":"s"}""");
        var (_, hidden, _) = FieldClassifier.Classify(doc.RootElement);

        Assert.Contains("PK", hidden);
        Assert.Contains("Document__Slug", hidden);
    }

    [Fact]
    public void Classify_CaseInsensitiveTitleMatch()
    {
        using var doc = JsonDocument.Parse("""{"NAME":"Goblin","level":3}""");
        var (titleField, _, _) = FieldClassifier.Classify(doc.RootElement);
        Assert.Equal("NAME", titleField);
    }

    [Fact]
    public void Classify_AllHidden_ReturnsEmptyRemaining()
    {
        using var doc = JsonDocument.Parse("""{"name":"X","pk":1,"model":"m","slug":"s"}""");
        var (_, hidden, remaining) = FieldClassifier.Classify(doc.RootElement);

        Assert.Equal(3, hidden.Count);
        Assert.Empty(remaining);
    }
}
