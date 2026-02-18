using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Chronicis.Api.Services.ExternalLinks;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class GenericJsonMarkdownRendererTests
{
    [Fact]
    public void RenderMarkdown_UsesFieldsNameAsTitle()
    {
        using var json = JsonDocument.Parse("""
        {
          "pk": "spells.fireball",
          "fields": {
            "name": "Fireball",
            "level": 3,
            "spellLevel": 3
          }
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "SRD 2014", "fallback");

        Assert.Contains("# Fireball", markdown);
        Assert.Contains("## Attributes", markdown);
        Assert.Contains("- **Level**: 3", markdown);
        Assert.Contains("- **Spell Level**: 3", markdown);
        Assert.DoesNotContain("Pk", markdown);
        Assert.DoesNotContain("**Name**", markdown);
        Assert.Contains("*Source: SRD 2014*", markdown);
    }

    [Fact]
    public void RenderMarkdown_UsesFallbackTitle_WhenNameMissing()
    {
        using var json = JsonDocument.Parse("""
        {
          "fields": {
            "desc": "Some text"
          }
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "Ros", "fallback-title");

        Assert.Contains("# fallback-title", markdown);
        Assert.Contains("- **Desc**: Some text", markdown);
    }

    [Fact]
    public void RenderMarkdown_UsesFallbackTitle_WhenNameIsWhitespace()
    {
        using var json = JsonDocument.Parse("""
        {
          "fields": {
            "name": "   ",
            "desc": "Some text"
          }
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "Ros", "fallback-title");

        Assert.Contains("# fallback-title", markdown);
    }

    [Fact]
    public void RenderMarkdown_EscapesAngleBrackets()
    {
        using var json = JsonDocument.Parse("""
        {
          "fields": {
            "name": "<script>alert(1)</script>",
            "desc": "<b>unsafe</b>"
          }
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "SRD", "fallback");

        Assert.Contains("# &lt;script&gt;alert(1)&lt;/script&gt;", markdown);
        Assert.Contains("&lt;b&gt;unsafe&lt;/b&gt;", markdown);
        Assert.DoesNotContain("<script>", markdown);
    }

    [Fact]
    public void RenderMarkdown_RendersNestedObjectsAndArrays()
    {
        using var json = JsonDocument.Parse("""
        {
          "fields": {
            "name": "Animated Armor",
            "armor_class": 18,
            "traits": ["construct", "medium"],
            "speed": { "walk": "25 ft" },
            "flags": [true, false],
            "aliases": [
              { "name": "Guardian Shell" }
            ],
            "notes": null
          }
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "SRD", "fallback");

        Assert.Contains("- **Armor Class**: 18", markdown);
        Assert.Contains("- **Traits**:", markdown);
        Assert.Contains("- construct", markdown);
        Assert.Contains("- medium", markdown);
        Assert.Contains("- **Speed**:", markdown);
        Assert.Contains("- **Walk**: 25 ft", markdown);
        Assert.Contains("- **Flags**:", markdown);
        Assert.Contains("- True", markdown);
        Assert.Contains("- False", markdown);
        Assert.Contains("- **Aliases**:", markdown);
        Assert.Contains("- **Name**: Guardian Shell", markdown);
        Assert.Contains("- **Notes**: null", markdown);
    }

    [Fact]
    public void RenderMarkdown_CoversTopLevelNonFieldsAndNestedArrayBranches()
    {
        using var json = JsonDocument.Parse("""
        {
          "fields": "not-an-object",
          "is_magic": true,
          "arr": [[1], null]
        }
        """);

        var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(json, "SRD", "fallback");

        Assert.Contains("# fallback", markdown);
        Assert.Contains("- **Fields**: not-an-object", markdown);
        Assert.Contains("- **Is Magic**: True", markdown);
        Assert.Contains("- (nested array)", markdown);
    }

    [Fact]
    public void PrivateHelpers_HandleEdgeCases()
    {
        var renderProperty = typeof(GenericJsonMarkdownRenderer).GetMethod(
            "RenderProperty",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var formatFieldName = typeof(GenericJsonMarkdownRenderer).GetMethod(
            "FormatFieldName",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var escapeMarkdown = typeof(GenericJsonMarkdownRenderer).GetMethod(
            "EscapeMarkdown",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var sb = new StringBuilder();
        renderProperty.Invoke(null, [sb, "undef", default(JsonElement), 0]);
        Assert.Equal(string.Empty, sb.ToString());

        var formatted = (string?)formatFieldName.Invoke(null, [""]);
        Assert.Equal(string.Empty, formatted);

        var escaped = (string?)escapeMarkdown.Invoke(null, [""]);
        Assert.Equal(string.Empty, escaped);
    }
}
