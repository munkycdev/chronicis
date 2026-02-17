using Chronicis.Client.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class MarkdownServiceTests
{
    private readonly ILogger<MarkdownService> _logger;
    private readonly MarkdownService _sut;

    public MarkdownServiceTests()
    {
        _logger = Substitute.For<ILogger<MarkdownService>>();
        _sut = new MarkdownService(_logger);
    }

    // ════════════════════════════════════════════════════════════════
    //  ToHtml Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ToHtml_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ToHtml(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToHtml_WithEmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ToHtml(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToHtml_WithWhitespaceInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ToHtml("   \n\t  ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToHtml_WithSimpleMarkdown_ConvertsToHtml()
    {
        // Arrange
        var markdown = "# Heading\n\nSome **bold** text.";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<h1", result);
        Assert.Contains("Heading", result);
        Assert.Contains("<strong>bold</strong>", result);
    }

    [Fact]
    public void ToHtml_WithUnorderedList_ConvertsToHtml()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<ul>", result);
        Assert.Contains("<li>Item 1</li>", result);
        Assert.Contains("<li>Item 2</li>", result);
        Assert.Contains("<li>Item 3</li>", result);
    }

    [Fact]
    public void ToHtml_WithOrderedList_ConvertsToHtml()
    {
        // Arrange
        var markdown = "1. First\n2. Second\n3. Third";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<ol>", result);
        Assert.Contains("<li>First</li>", result);
        Assert.Contains("<li>Second</li>", result);
        Assert.Contains("<li>Third</li>", result);
    }

    [Fact]
    public void ToHtml_WithCodeBlock_ConvertsToHtml()
    {
        // Arrange
        var markdown = "```csharp\nvar x = 42;\n```";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<pre>", result);
        Assert.Contains("<code", result);
        Assert.Contains("var x = 42;", result);
    }

    [Fact]
    public void ToHtml_WithLink_ConvertsToHtml()
    {
        // Arrange
        var markdown = "[Google](https://google.com)";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<a href=\"https://google.com\">Google</a>", result);
    }

    [Fact]
    public void ToHtml_WithTable_ConvertsToHtml()
    {
        // Arrange
        var markdown = "| Column 1 | Column 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<table>", result);
        Assert.Contains("<thead>", result);
        Assert.Contains("<tbody>", result);
        Assert.Contains("Column 1", result);
        Assert.Contains("Cell 1", result);
    }

    [Fact]
    public void ToHtml_SanitizesScriptTags()
    {
        // Arrange
        var markdown = "Normal text <script>alert('xss')</script>";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("alert", result);
    }

    [Fact]
    public void ToHtml_AllowsImageTags()
    {
        // Arrange
        var markdown = "![Alt text](https://example.com/image.png)";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        Assert.Contains("<img", result);
        Assert.Contains("src=\"https://example.com/image.png\"", result);
        Assert.Contains("alt=\"Alt text\"", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  ToPlainText Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ToPlainText_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ToPlainText(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToPlainText_WithEmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ToPlainText(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToPlainText_RemovesMarkdownFormatting()
    {
        // Arrange
        var markdown = "# Heading\n\nSome **bold** and *italic* text.";

        // Act
        var result = _sut.ToPlainText(markdown);

        // Assert
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("*", result);
        Assert.Contains("Heading", result);
        Assert.Contains("bold", result);
        Assert.Contains("italic", result);
    }

    [Fact]
    public void ToPlainText_RemovesLinks()
    {
        // Arrange
        var markdown = "[Google](https://google.com)";

        // Act
        var result = _sut.ToPlainText(markdown);

        // Assert
        Assert.Contains("Google", result);
        Assert.DoesNotContain("https://google.com", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  GetPreview Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void GetPreview_WithShortText_ReturnsFullText()
    {
        // Arrange
        var markdown = "Short text";

        // Act
        var result = _sut.GetPreview(markdown, maxLength: 200);

        // Assert
        Assert.Equal("Short text\n", result); // ToPlainText adds newline
        Assert.DoesNotContain("...", result);
    }

    [Fact]
    public void GetPreview_WithLongText_TruncatesAndAddsEllipsis()
    {
        // Arrange
        var markdown = "This is a very long piece of text that should be truncated to the specified maximum length and have ellipsis added at the end.";

        // Act
        var result = _sut.GetPreview(markdown, maxLength: 50);

        // Assert
        Assert.Equal(53, result.Length); // 50 chars + "..."
        Assert.EndsWith("...", result);
        Assert.StartsWith("This is a very long", result);
    }

    [Fact]
    public void GetPreview_RemovesMarkdownFormatting()
    {
        // Arrange
        var markdown = "# Heading\n\n**Bold text** with *emphasis*.";

        // Act
        var result = _sut.GetPreview(markdown, maxLength: 200);

        // Assert
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void GetPreview_WithDefaultMaxLength_Uses200Characters()
    {
        // Arrange
        var markdown = new string('a', 250);

        // Act
        var result = _sut.GetPreview(markdown);

        // Assert
        Assert.Equal(203, result.Length); // 200 + "..."
        Assert.EndsWith("...", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  IsHtml Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsHtml_WithNullInput_ReturnsFalse()
    {
        // Act
        var result = _sut.IsHtml(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHtml_WithEmptyInput_ReturnsFalse()
    {
        // Act
        var result = _sut.IsHtml(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHtml_WithPlainText_ReturnsFalse()
    {
        // Act
        var result = _sut.IsHtml("Just some plain text");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHtml_WithMarkdown_ReturnsFalse()
    {
        // Act
        var result = _sut.IsHtml("# Heading\n\n**Bold** text");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHtml_WithParagraphTag_ReturnsTrue()
    {
        // Act
        var result = _sut.IsHtml("<p>Some text</p>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtml_WithHeadingTag_ReturnsTrue()
    {
        // Act
        var result = _sut.IsHtml("<h1>Heading</h1>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtml_WithDivTag_ReturnsTrue()
    {
        // Act
        var result = _sut.IsHtml("<div>Content</div>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtml_WithListTags_ReturnsTrue()
    {
        // Act
        var result = _sut.IsHtml("<ul><li>Item</li></ul>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHtml_CaseInsensitive_ReturnsTrue()
    {
        // Act
        var result = _sut.IsHtml("<P>Text</P>");

        // Assert
        Assert.True(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  EnsureHtml Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void EnsureHtml_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.EnsureHtml(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EnsureHtml_WithEmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = _sut.EnsureHtml(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EnsureHtml_WithHtmlInput_ReturnsSanitizedHtml()
    {
        // Arrange
        var html = "<p>Some text</p>";

        // Act
        var result = _sut.EnsureHtml(html);

        // Assert
        Assert.Contains("<p>", result);
        Assert.Contains("Some text", result);
    }

    [Fact]
    public void EnsureHtml_WithMarkdownInput_ConvertsToHtml()
    {
        // Arrange
        var markdown = "# Heading\n\n**Bold** text";

        // Act
        var result = _sut.EnsureHtml(markdown);

        // Assert
        Assert.Contains("<h1", result);
        Assert.Contains("<strong>", result);
    }

    [Fact]
    public void EnsureHtml_SanitizesHtmlInput()
    {
        // Arrange
        var html = "<p>Text</p><script>alert('xss')</script>";

        // Act
        var result = _sut.EnsureHtml(html);

        // Assert
        Assert.Contains("<p>", result);
        Assert.DoesNotContain("<script>", result);
    }

    [Fact]
    public void EnsureHtml_WithPlainText_WrapsInParagraph()
    {
        // Arrange
        var plainText = "Just some plain text";

        // Act
        var result = _sut.EnsureHtml(plainText);

        // Assert
        Assert.Contains("<p>", result);
        Assert.Contains("Just some plain text", result);
    }
}
