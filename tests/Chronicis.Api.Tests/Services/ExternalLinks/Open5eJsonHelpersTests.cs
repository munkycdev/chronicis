using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class Open5eJsonHelpersTests
{
    [Fact]
    public void GetString_ReturnsStringValue()
    {
        using var doc = JsonDocument.Parse("""{"name":"Fireball"}""");
        Assert.Equal("Fireball", Open5eJsonHelpers.GetString(doc.RootElement, "name"));
    }

    [Fact]
    public void GetString_NonStringProperty_ReturnsToString()
    {
        using var doc = JsonDocument.Parse("""{"level":3}""");
        Assert.Equal("3", Open5eJsonHelpers.GetString(doc.RootElement, "level"));
    }

    [Fact]
    public void GetString_MissingProperty_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Null(Open5eJsonHelpers.GetString(doc.RootElement, "missing"));
    }

    [Fact]
    public void GetString_NonObjectElement_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");
        Assert.Null(Open5eJsonHelpers.GetString(doc.RootElement, "name"));
    }

    [Fact]
    public void GetInt_ReturnsIntValue()
    {
        using var doc = JsonDocument.Parse("""{"n":42}""");
        Assert.Equal(42, Open5eJsonHelpers.GetInt(doc.RootElement, "n"));
    }

    [Fact]
    public void GetInt_MissingProperty_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Null(Open5eJsonHelpers.GetInt(doc.RootElement, "n"));
    }

    [Fact]
    public void GetInt_NonObjectElement_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");
        Assert.Null(Open5eJsonHelpers.GetInt(doc.RootElement, "n"));
    }

    [Fact]
    public void GetInt_OverflowValue_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"n":2147483648}""");
        Assert.Null(Open5eJsonHelpers.GetInt(doc.RootElement, "n"));
    }

    [Fact]
    public void GetInt_StringValue_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"n":"42"}""");
        Assert.Null(Open5eJsonHelpers.GetInt(doc.RootElement, "n"));
    }

    [Fact]
    public void GetStringFromObject_ReturnsChildValue()
    {
        using var doc = JsonDocument.Parse("""{"obj":{"name":"Inside"}}""");
        Assert.Equal("Inside", Open5eJsonHelpers.GetStringFromObject(doc.RootElement, "obj", "name"));
    }

    [Fact]
    public void GetStringFromObject_MissingParent_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Null(Open5eJsonHelpers.GetStringFromObject(doc.RootElement, "obj", "name"));
    }

    [Fact]
    public void GetStringFromObject_NonObjectElement_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");
        Assert.Null(Open5eJsonHelpers.GetStringFromObject(doc.RootElement, "obj", "name"));
    }

    [Theory]
    [InlineData("""{"a":true}""", "a", true)]
    [InlineData("""{"a":false}""", "a", false)]
    [InlineData("""{"a":"yes"}""", "a", true)]
    [InlineData("""{"a":"true"}""", "a", true)]
    [InlineData("""{"a":"no"}""", "a", false)]
    [InlineData("""{"a":"something"}""", "a", false)]
    public void GetBool_VariousValues(string json, string prop, bool expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, Open5eJsonHelpers.GetBool(doc.RootElement, prop));
    }

    [Fact]
    public void GetBool_MissingProperty_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Null(Open5eJsonHelpers.GetBool(doc.RootElement, "missing"));
    }

    [Fact]
    public void GetBool_NumberValue_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"n":123}""");
        Assert.Null(Open5eJsonHelpers.GetBool(doc.RootElement, "n"));
    }

    [Fact]
    public void GetBool_NonObjectElement_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");
        Assert.Null(Open5eJsonHelpers.GetBool(doc.RootElement, "x"));
    }

    [Fact]
    public void GetStringArray_ArrayOfStrings()
    {
        using var doc = JsonDocument.Parse("""{"props":["Versatile","Heavy"]}""");
        var result = Open5eJsonHelpers.GetStringArray(doc.RootElement, "props");
        Assert.Equal(2, result.Count);
        Assert.Equal("Versatile", result[0]);
    }

    [Fact]
    public void GetStringArray_ArrayOfObjects_ExtractsName()
    {
        using var doc = JsonDocument.Parse("""{"props":[{"name":"Heavy"},{"name":"Finesse"}]}""");
        var result = Open5eJsonHelpers.GetStringArray(doc.RootElement, "props");
        Assert.Equal(2, result.Count);
        Assert.Equal("Heavy", result[0]);
    }

    [Fact]
    public void GetStringArray_MixedArray_SkipsNullsAndEmpty()
    {
        using var doc = JsonDocument.Parse("""{"props":["Versatile",{"name":"Heavy"},null,{"name":""},"",{"other":"val"}]}""");
        var result = Open5eJsonHelpers.GetStringArray(doc.RootElement, "props");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetStringArray_SingleStringValue()
    {
        using var doc = JsonDocument.Parse("""{"props":"Versatile"}""");
        var result = Open5eJsonHelpers.GetStringArray(doc.RootElement, "props");
        Assert.Single(result);
        Assert.Equal("Versatile", result[0]);
    }

    [Fact]
    public void GetStringArray_MissingProperty_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Empty(Open5eJsonHelpers.GetStringArray(doc.RootElement, "missing"));
    }

    [Fact]
    public void GetStringArray_NonObjectElement_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""[1,2,3]""");
        Assert.Empty(Open5eJsonHelpers.GetStringArray(doc.RootElement, "props"));
    }

    [Fact]
    public void GetStringArray_EmptyStringValue_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"props":""}""");
        Assert.Empty(Open5eJsonHelpers.GetStringArray(doc.RootElement, "props"));
    }

    [Fact]
    public void GetSpeedString_ObjectSpeed_ReturnsFormatted()
    {
        using var doc = JsonDocument.Parse("""{"speed":{"walk":"30 ft","swim":"20 ft"}}""");
        var result = Open5eJsonHelpers.GetSpeedString(doc.RootElement);
        Assert.Equal("walk 30 ft, swim 20 ft", result);
    }

    [Fact]
    public void GetSpeedString_ObjectSpeedWithNumbers_ReturnsFormatted()
    {
        using var doc = JsonDocument.Parse("""{"speed":{"walk":30}}""");
        Assert.Equal("walk 30", Open5eJsonHelpers.GetSpeedString(doc.RootElement));
    }

    [Fact]
    public void GetSpeedString_StringSpeed_ReturnsDirect()
    {
        using var doc = JsonDocument.Parse("""{"speed":"30 ft"}""");
        Assert.Equal("30 ft", Open5eJsonHelpers.GetSpeedString(doc.RootElement));
    }

    [Fact]
    public void GetSpeedString_MissingSpeed_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Null(Open5eJsonHelpers.GetSpeedString(doc.RootElement));
    }

    [Fact]
    public void AppendNamedArray_WithItems_AppendsFormatted()
    {
        using var doc = JsonDocument.Parse("""{"actions":[{"name":"Bite","desc":"Attack."},{"name":"Claw","desc":"Swipe."}]}""");
        var sb = new StringBuilder();
        Open5eJsonHelpers.AppendNamedArray(sb, doc.RootElement, "actions", "Actions");
        var result = sb.ToString();
        Assert.Contains("## Actions", result);
        Assert.Contains("### Bite", result);
        Assert.Contains("### Claw", result);
    }

    [Fact]
    public void AppendNamedArray_MissingField_NoOutput()
    {
        using var doc = JsonDocument.Parse("""{}""");
        var sb = new StringBuilder();
        Open5eJsonHelpers.AppendNamedArray(sb, doc.RootElement, "actions", "Actions");
        Assert.Equal(string.Empty, sb.ToString());
    }

    [Fact]
    public void AppendNamedArray_EmptyArray_NoOutput()
    {
        using var doc = JsonDocument.Parse("""{"actions":[]}""");
        var sb = new StringBuilder();
        Open5eJsonHelpers.AppendNamedArray(sb, doc.RootElement, "actions", "Actions");
        Assert.Equal(string.Empty, sb.ToString());
    }

    [Fact]
    public void AppendNamedArray_NonArrayField_NoOutput()
    {
        using var doc = JsonDocument.Parse("""{"actions":"not-an-array"}""");
        var sb = new StringBuilder();
        Open5eJsonHelpers.AppendNamedArray(sb, doc.RootElement, "actions", "Actions");
        Assert.Equal(string.Empty, sb.ToString());
    }

    [Fact]
    public void BuildAttribution_WithDocumentObject_ReturnsDocName()
    {
        using var doc = JsonDocument.Parse("""{"document":{"name":"SRD 5.1"}}""");
        Assert.Equal("Source: SRD 5.1", Open5eJsonHelpers.BuildAttribution(doc.RootElement));
    }

    [Fact]
    public void BuildAttribution_WithDocumentTitle_ReturnsFallback()
    {
        using var doc = JsonDocument.Parse("""{"document__title":"Custom SRD"}""");
        Assert.Equal("Source: Custom SRD", Open5eJsonHelpers.BuildAttribution(doc.RootElement));
    }

    [Fact]
    public void BuildAttribution_NoDocumentFields_ReturnsDefault()
    {
        using var doc = JsonDocument.Parse("""{"name":"x"}""");
        Assert.Equal("Source: System Reference Document 5.1", Open5eJsonHelpers.BuildAttribution(doc.RootElement));
    }
}
