namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the LogSanitizer utility class.
/// Validates log injection prevention and input sanitization for secure logging.
/// </summary>
public class LogSanitizerTests
{
    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Clean Input (No Sanitization Needed)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_CleanString_ReturnsUnchanged()
    {
        var input = "Hello World";
        var result = LogSanitizer.Sanitize(input);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_AlphanumericOnly_ReturnsUnchanged()
    {
        var input = "Test123";
        var result = LogSanitizer.Sanitize(input);
        Assert.Equal("Test123", result);
    }

    [Fact]
    public void Sanitize_BasicPunctuation_ReturnsUnchanged()
    {
        var input = "Hello, World!";
        var result = LogSanitizer.Sanitize(input);
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Sanitize_SingleSpace_ReturnsUnchanged()
    {
        var input = "One Two Three";
        var result = LogSanitizer.Sanitize(input);
        Assert.Equal("One Two Three", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Null and Empty Input
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_NullInput_ReturnsNull()
    {
        var result = LogSanitizer.Sanitize(null);
        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsEmpty()
    {
        var result = LogSanitizer.Sanitize("");
        Assert.Equal("", result);
    }

    [Fact]
    public void Sanitize_WhitespaceOnly_ReturnsEmpty()
    {
        var result = LogSanitizer.Sanitize("   ");
        Assert.Equal("", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Control Characters
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_NewlineCharacter_RemovesNewline()
    {
        var input = "Line1\nLine2";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\n", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_CarriageReturn_RemovesCarriageReturn()
    {
        var input = "Line1\rLine2";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\r", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_Tab_RemovesTab()
    {
        var input = "Column1\tColumn2";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\t", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_CarriageReturnNewline_RemovesCRLF()
    {
        var input = "Line1\r\nLine2";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\r\n", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_LowAsciiControlCharacters_GetsSanitized()
    {
        // Test that lower ASCII control characters result in sanitization
        var input = "Test\x01\x02\x03Value";
        var result = LogSanitizer.Sanitize(input);
        
        // The string gets modified and marked as sanitized
        Assert.Contains("[SANITIZED]", result);
        // Result should contain TestValue but possibly with some control chars removed
        Assert.Contains("Test", result);
        Assert.Contains("Value", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - URL-Encoded Dangerous Characters
    // ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Test%0AValue", "%0A")]
    [InlineData("Test%0aValue", "%0a")]
    [InlineData("Test%0DValue", "%0D")]
    [InlineData("Test%0dValue", "%0d")]
    public void Sanitize_UrlEncodedNewlines_RemovesThem(string input, string encoded)
    {
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain(encoded, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_MultipleUrlEncodedCharacters_RemovesAll()
    {
        var input = "Test%0ALine%0DBreak%0aAgain";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("%0A", result);
        Assert.DoesNotContain("%0D", result);
        Assert.DoesNotContain("%0a", result);
        Assert.Contains("[SANITIZED]", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Multiple Whitespace
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_MultipleSpaces_CollapsesToSingle()
    {
        var input = "Hello    World";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal("Hello World [SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_ManySpaces_CollapsesToSingle()
    {
        var input = "Test          Value";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal("Test Value [SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_LeadingSpaces_Trims()
    {
        var input = "     Hello";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal("Hello [SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_TrailingSpaces_Trims()
    {
        var input = "Hello     ";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal("Hello [SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_LeadingAndTrailingSpaces_TrimsBoth()
    {
        var input = "   Hello   ";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal("Hello [SANITIZED]", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Truncation
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_Under1000Chars_NotTruncated()
    {
        var input = new string('a', 999);
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal(999, result!.Length);
        Assert.DoesNotContain("[TRUNCATED]", result);
    }

    [Fact]
    public void Sanitize_Exactly1000Chars_NotTruncated()
    {
        var input = new string('a', 1000);
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Equal(1000, result!.Length);
        Assert.DoesNotContain("[TRUNCATED]", result);
    }

    [Fact]
    public void Sanitize_Over1000Chars_IsTruncated()
    {
        var input = new string('a', 1500);
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Contains("[TRUNCATED]", result);
        Assert.Contains("[SANITIZED]", result);
        Assert.True(result!.Length < 1500);
    }

    [Fact]
    public void Sanitize_VeryLongString_TruncatesTo1000Plus()
    {
        var input = new string('a', 5000);
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Contains("[TRUNCATED]", result);
        // Should be approximately 1000 + marker length
        Assert.True(result!.Length < 1100);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Marker Application
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_ModifiedContent_AddsSanitizedMarker()
    {
        var input = "Hello\nWorld";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_UnmodifiedContent_NoMarker()
    {
        var input = "Clean text";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("[SANITIZED]", result);
        Assert.DoesNotContain("[TRUNCATED]", result);
    }

    [Fact]
    public void Sanitize_TruncatedAndSanitized_HasBothMarkers()
    {
        var input = new string('a', 1001) + "\n";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.Contains("[TRUNCATED]", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_OnlyWhitespaceModified_NoMarkerOnEmpty()
    {
        var input = "   \n   ";
        var result = LogSanitizer.Sanitize(input);
        
        // After trimming and removing newlines, result is empty
        Assert.Equal("", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  Sanitize - Complex Scenarios
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Sanitize_LogInjectionAttempt_RemovesDangerousContent()
    {
        var input = "User login\nINFO: Admin password: secret123";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\n", result);
        Assert.Contains("[SANITIZED]", result);
        Assert.Contains("User login", result);
        Assert.Contains("INFO: Admin password: secret123", result);
    }

    [Fact]
    public void Sanitize_MixedControlCharacters_RemovesCommonOnes()
    {
        var input = "Test\r\n\tValue";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\t", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void Sanitize_MixedSanitizationNeeds_HandlesAll()
    {
        var input = "  Hello\n\nWorld  \t  Test  ";
        var result = LogSanitizer.Sanitize(input);
        
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\t", result);
        Assert.DoesNotContain("  ", result); // Multiple spaces collapsed
        Assert.Contains("[SANITIZED]", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  SanitizeObject
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeObject_NullObject_ReturnsNull()
    {
        var result = LogSanitizer.SanitizeObject(null);
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeObject_StringObject_SanitizesString()
    {
        object input = "Hello\nWorld";
        var result = LogSanitizer.SanitizeObject(input);
        
        Assert.DoesNotContain("\n", result);
        Assert.Contains("[SANITIZED]", result);
    }

    [Fact]
    public void SanitizeObject_IntObject_ConvertsToString()
    {
        object input = 12345;
        var result = LogSanitizer.SanitizeObject(input);
        
        Assert.Equal("12345", result);
    }

    [Fact]
    public void SanitizeObject_BoolObject_ConvertsToString()
    {
        object input = true;
        var result = LogSanitizer.SanitizeObject(input);
        
        Assert.Equal("True", result);
    }

    [Fact]
    public void SanitizeObject_DateTimeObject_ConvertsToString()
    {
        object input = new DateTime(2025, 1, 1);
        var result = LogSanitizer.SanitizeObject(input);
        
        Assert.Contains("2025", result);
    }

    [Fact]
    public void SanitizeObject_CustomObject_UsesToString()
    {
        var input = new { Name = "Test\nUser", Id = 123 };
        var result = LogSanitizer.SanitizeObject(input);
        
        // ToString on anonymous objects includes type info
        Assert.NotNull(result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void SanitizeObject_ObjectWithDangerousToString_Sanitizes()
    {
        object input = new TestObjectWithDangerousToString();
        var result = LogSanitizer.SanitizeObject(input);
        
        Assert.DoesNotContain("\n", result);
        Assert.Contains("[SANITIZED]", result);
    }

    // ────────────────────────────────────────────────────────────────
    //  SanitizeMultiple
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeMultiple_EmptyArray_ReturnsEmpty()
    {
        var result = LogSanitizer.SanitizeMultiple();
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeMultiple_SingleValue_SanitizesIt()
    {
        var result = LogSanitizer.SanitizeMultiple("Hello\nWorld");
        
        Assert.Single(result);
        Assert.DoesNotContain("\n", result[0]);
        Assert.Contains("[SANITIZED]", result[0]);
    }

    [Fact]
    public void SanitizeMultiple_MultipleCleanValues_ReturnsUnchanged()
    {
        var result = LogSanitizer.SanitizeMultiple("Value1", "Value2", "Value3");
        
        Assert.Equal(3, result.Length);
        Assert.Equal("Value1", result[0]);
        Assert.Equal("Value2", result[1]);
        Assert.Equal("Value3", result[2]);
    }

    [Fact]
    public void SanitizeMultiple_MixOfCleanAndDirty_SanitizesOnlyDirty()
    {
        var result = LogSanitizer.SanitizeMultiple("Clean", "Dirty\nValue", "AlsoClean");
        
        Assert.Equal(3, result.Length);
        Assert.Equal("Clean", result[0]);
        Assert.DoesNotContain("\n", result[1]);
        Assert.Contains("[SANITIZED]", result[1]);
        Assert.Equal("AlsoClean", result[2]);
    }

    [Fact]
    public void SanitizeMultiple_WithNulls_HandlesNulls()
    {
        var result = LogSanitizer.SanitizeMultiple("Value1", null, "Value3");
        
        Assert.Equal(3, result.Length);
        Assert.Equal("Value1", result[0]);
        Assert.Null(result[1]);
        Assert.Equal("Value3", result[2]);
    }

    [Fact]
    public void SanitizeMultiple_AllDirtyValues_SanitizesAll()
    {
        var result = LogSanitizer.SanitizeMultiple("Test\n1", "Test\r2", "Test\t3");
        
        Assert.Equal(3, result.Length);
        Assert.All(result, r => Assert.Contains("[SANITIZED]", r));
        Assert.All(result, r => Assert.DoesNotContain("\n", r));
        Assert.All(result, r => Assert.DoesNotContain("\r", r));
        Assert.All(result, r => Assert.DoesNotContain("\t", r));
    }

    // Helper class for testing
    private class TestObjectWithDangerousToString
    {
        public override string ToString()
        {
            return "Dangerous\nContent\rWith\tControls";
        }
    }
}
