using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Utilities;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class LogSanitizerTests
{
    // --- Sanitize ---

    [Fact]
    public void Sanitize_ReturnsNull_WhenInputIsNull()
    {
        Assert.Null(LogSanitizer.Sanitize(null));
    }

    [Fact]
    public void Sanitize_ReturnsEmpty_WhenInputIsEmpty()
    {
        Assert.Equal(string.Empty, LogSanitizer.Sanitize(string.Empty));
    }

    [Fact]
    public void Sanitize_ReturnsCleanStringUnmodified()
    {
        var input = "Hello world";
        var result = LogSanitizer.Sanitize(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_RemovesNewlines()
    {
        var input = "Line1\nLine2\r\nLine3";
        var result = LogSanitizer.Sanitize(input);

        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\r", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_RemovesTabCharacters()
    {
        var input = "Col1\tCol2";
        var result = LogSanitizer.Sanitize(input);

        Assert.DoesNotContain("\t", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_RemovesUrlEncodedNewlines()
    {
        var input = "injected%0Alog line";
        var result = LogSanitizer.Sanitize(input);

        Assert.DoesNotContain("%0A", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_CollapsesMultipleSpaces()
    {
        var input = "too    many     spaces";
        var result = LogSanitizer.Sanitize(input);

        Assert.DoesNotContain("  ", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_TruncatesLongInput()
    {
        var input = new string('A', 2000);
        var result = LogSanitizer.Sanitize(input);

        // Max 1000 chars + truncation marker + sanitized marker
        Assert.Contains("[TRUNCATED]", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_DoesNotAddSanitizedMarker_WhenClean()
    {
        var result = LogSanitizer.Sanitize("clean input");

        Assert.DoesNotContain("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_RemovesNullCharacters()
    {
        var input = "hello\0world";
        var result = LogSanitizer.Sanitize(input);

        Assert.NotNull(result);
        Assert.Contains("helloworld", result);
        Assert.Contains("[SANITIZED]", result);
    }

    // --- SanitizeObject ---

    [Fact]
    public void SanitizeObject_ReturnsNull_WhenInputIsNull()
    {
        Assert.Null(LogSanitizer.SanitizeObject(null));
    }

    [Fact]
    public void SanitizeObject_SanitizesToStringOutput()
    {
        var result = LogSanitizer.SanitizeObject(42);

        Assert.Equal("42", result);
    }

    [Fact]
    public void SanitizeObject_SanitizesObjectWithControlChars()
    {
        var obj = new { Name = "test\ninjection" };
        var result = LogSanitizer.SanitizeObject(obj);

        Assert.DoesNotContain("\n", result);
        Assert.Contains("[SANITIZED]", result);
    }

    // --- SanitizeMultiple ---

    [Fact]
    public void SanitizeMultiple_SanitizesAllValues()
    {
        var results = LogSanitizer.SanitizeMultiple("clean", "has\nnewline", null);

        Assert.Equal(3, results.Length);
        Assert.Equal("clean", results[0]);
        Assert.Contains("[SANITIZED]", results[1]);
        Assert.Null(results[2]);
    }

    [Fact]
    public void SanitizeMultiple_ReturnsEmptyArray_WhenNoInputs()
    {
        var results = LogSanitizer.SanitizeMultiple();

        Assert.Empty(results);
    }
}
