using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class HtmlToMarkdownConverterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    public void Convert_ReturnsEmpty_ForBlankInput(string? input, string expected)
    {
        Assert.Equal(expected, HtmlToMarkdownConverter.Convert(input!));
    }

    [Fact]
    public void Convert_ConvertsHeaders()
    {
        Assert.Contains("# Title", HtmlToMarkdownConverter.Convert("<h1>Title</h1>"));
        Assert.Contains("## Sub", HtmlToMarkdownConverter.Convert("<h2>Sub</h2>"));
        Assert.Contains("### Third", HtmlToMarkdownConverter.Convert("<h3>Third</h3>"));
        Assert.Contains("###### Sixth", HtmlToMarkdownConverter.Convert("<h6>Sixth</h6>"));
    }

    [Fact]
    public void ConvertHeaders_LoopCoversAllSixLevels()
    {
        for (int i = 1; i <= 6; i++)
        {
            var prefix = new string('#', i);
            var html = $"<h{i}>Level {i}</h{i}>";
            Assert.Contains($"{prefix} Level {i}", HtmlToMarkdownConverter.ConvertHeaders(html));
        }
    }

    [Fact]
    public void Convert_ConvertsBoldAndItalic()
    {
        var result = HtmlToMarkdownConverter.Convert("<p><strong>bold</strong> and <em>italic</em></p>");
        Assert.Contains("**bold**", result);
        Assert.Contains("*italic*", result);
    }

    [Fact]
    public void Convert_ConvertsBAndITags()
    {
        var result = HtmlToMarkdownConverter.ConvertInlineFormatting("<b>bold</b> <i>italic</i>");
        Assert.Contains("**bold**", result);
        Assert.Contains("*italic*", result);
    }

    [Fact]
    public void Convert_ConvertsLinks()
    {
        var result = HtmlToMarkdownConverter.Convert("<a href=\"https://example.com\">link</a>");
        Assert.Contains("[link](https://example.com)", result);
    }

    [Fact]
    public void Convert_ConvertsWikiLinks_WithDisplay()
    {
        var html = "<span data-type=\"wiki-link\" data-target-id=\"abc\" data-display=\"Waterdeep\">text</span>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("[[Waterdeep]]", result);
    }

    [Fact]
    public void Convert_ConvertsWikiLinks_WithoutDisplay()
    {
        var html = "<span data-type=\"wiki-link\" data-target-id=\"abc\">Waterdeep</span>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("[[Waterdeep]]", result);
    }

    [Fact]
    public void Convert_ConvertsCodeBlocks()
    {
        var html = "<pre><code>var x = 1;</code></pre>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("```", result);
        Assert.Contains("var x = 1;", result);
    }

    [Fact]
    public void Convert_ConvertsInlineCode()
    {
        var result = HtmlToMarkdownConverter.Convert("<code>inline</code>");
        Assert.Contains("`inline`", result);
    }

    [Fact]
    public void Convert_ConvertsBlockquotes()
    {
        var html = "<blockquote><p>quoted text</p></blockquote>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("> quoted text", result);
    }

    [Fact]
    public void Convert_ConvertsUnorderedLists()
    {
        var html = "<ul><li>one</li><li>two</li></ul>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("- one", result);
        Assert.Contains("- two", result);
    }

    [Fact]
    public void Convert_ConvertsOrderedLists()
    {
        var html = "<ol><li>first</li><li>second</li></ol>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("1. first", result);
        Assert.Contains("2. second", result);
    }

    [Fact]
    public void Convert_ConvertsNestedUnorderedLists()
    {
        var html = "<ul><li>parent<ul><li>child</li></ul></li></ul>";
        var result = HtmlToMarkdownConverter.Convert(html);
        // Debug: see what we actually get
        Assert.True(result.Contains("- parent"), $"Expected '- parent' in:\n{result}");
        Assert.True(result.Contains("  - child"), $"Expected '  - child' in:\n{result}");
    }

    [Fact]
    public void Convert_ConvertsNestedOrderedLists()
    {
        var html = "<ol><li>parent<ol><li>child</li></ol></li></ol>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.Contains("1. parent", result);
        Assert.Contains("  1. child", result);
    }

    [Fact]
    public void ProcessList_HandlesItemWithNoNestedList()
    {
        var result = HtmlToMarkdownConverter.ProcessList("<li>plain item</li>", ordered: false, indentLevel: 0);
        Assert.Contains("- plain item", result);
    }

    [Fact]
    public void ProcessList_HandlesBrokenNestedList()
    {
        // Nested tag present but no closing — SplitNestedList won't match
        var result = HtmlToMarkdownConverter.ProcessList("<li>broken<ul>no close</li>", ordered: false, indentLevel: 0);
        Assert.Contains("- broken", result);
    }

    [Fact]
    public void ProcessList_OrderedWithIndent()
    {
        var result = HtmlToMarkdownConverter.ProcessList("<li>indented</li>", ordered: true, indentLevel: 1);
        Assert.Contains("  1. indented", result);
    }

    [Fact]
    public void Convert_ConvertsParagraphs()
    {
        var result = HtmlToMarkdownConverter.Convert("<p>paragraph</p>");
        Assert.Contains("paragraph", result);
    }

    [Fact]
    public void Convert_ConvertsLineBreaks()
    {
        var result = HtmlToMarkdownConverter.ConvertParagraphsAndBreaks("a<br/>b");
        Assert.Contains("a\nb", result);
    }

    [Fact]
    public void Convert_ConvertsHorizontalRules()
    {
        var result = HtmlToMarkdownConverter.ConvertParagraphsAndBreaks("a<hr/>b");
        Assert.Contains("---", result);
    }

    [Fact]
    public void Convert_StripsUnknownHtmlTags()
    {
        var result = HtmlToMarkdownConverter.Convert("<div>content</div>");
        Assert.Contains("content", result);
        Assert.DoesNotContain("<div>", result);
    }

    [Fact]
    public void Convert_DecodesHtmlEntities()
    {
        var result = HtmlToMarkdownConverter.Convert("<p>a &amp; b &lt; c</p>");
        Assert.Contains("a & b < c", result);
    }

    [Fact]
    public void Convert_NormalizesExcessiveNewlines()
    {
        Assert.Equal("a\n\nb", HtmlToMarkdownConverter.NormalizeWhitespace("a\n\n\n\nb"));
    }

    [Fact]
    public void Convert_ComplexDocument()
    {
        var html = @"<h1>Header</h1>
<p>Text with <strong>bold</strong> and <i>italic</i></p>
<ul><li>one</li><li>two<ul><li>inner</li></ul></li></ul>
<ol><li>first</li><li>second<ol><li>inner ordered</li></ol></li></ol>
<blockquote><p>quote</p></blockquote>";

        var result = HtmlToMarkdownConverter.Convert(html);

        Assert.Contains("# Header", result);
        Assert.Contains("**bold**", result);
        Assert.Contains("- one", result);
        Assert.Contains("1. first", result);
        Assert.Contains("> quote", result);
    }

    [Fact]
    public void Convert_HandlesListItemWithoutClosingTag()
    {
        // Exercises the break in ExtractListItems when </li> is missing
        var html = "<ul><li>no closing tag</ul>";
        var result = HtmlToMarkdownConverter.Convert(html);
        Assert.DoesNotContain("<li>", result);
    }
}
