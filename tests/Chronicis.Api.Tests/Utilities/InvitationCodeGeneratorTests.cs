using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Utilities;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class InvitationCodeGeneratorTests
{
    // --- GenerateCode ---

    [Fact]
    public void GenerateCode_ReturnsNineCharacterStringWithHyphen()
    {
        var code = InvitationCodeGenerator.GenerateCode();

        Assert.Equal(9, code.Length);
        Assert.Equal('-', code[4]);
    }

    [Fact]
    public void GenerateCode_MatchesExpectedFormat()
    {
        var code = InvitationCodeGenerator.GenerateCode();

        Assert.Matches(@"^[A-Z]{4}-[A-Z]{4}$", code);
    }

    [Fact]
    public void GenerateCode_UsesOnlyLetters()
    {
        // Generate several to exercise randomness
        for (int i = 0; i < 50; i++)
        {
            var code = InvitationCodeGenerator.GenerateCode();
            var letters = code.Replace("-", "");
            Assert.All(letters.ToCharArray(), c => Assert.True(char.IsLetter(c)));
        }
    }

    [Fact]
    public void GenerateCode_ProducesVariedResults()
    {
        var codes = Enumerable.Range(0, 20)
            .Select(_ => InvitationCodeGenerator.GenerateCode())
            .ToHashSet();

        // With cryptographic randomness, 20 codes should not all be identical
        Assert.True(codes.Count > 1, "Expected at least 2 unique codes from 20 generations");
    }

    [Fact]
    public void GenerateCode_UsesOnlyExpectedCharacters()
    {
        var allowedChars = "ABCDEFGHIJKLMNOPRSTUVWXZ".ToHashSet(); // Note: no Q, Y per Consonants+Vowels

        for (int i = 0; i < 50; i++)
        {
            var code = InvitationCodeGenerator.GenerateCode();
            var letters = code.Replace("-", "");
            Assert.All(letters.ToCharArray(), c =>
                Assert.True(allowedChars.Contains(c), $"Unexpected character '{c}' in code '{code}'"));
        }
    }

    // --- NormalizeCode ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCode_ReturnsEmptyForNullOrWhitespace(string? input)
    {
        var result = InvitationCodeGenerator.NormalizeCode(input!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeCode_UppercasesInput()
    {
        var result = InvitationCodeGenerator.NormalizeCode("abcd-efgh");

        Assert.Equal("ABCD-EFGH", result);
    }

    [Fact]
    public void NormalizeCode_TrimsWhitespace()
    {
        var result = InvitationCodeGenerator.NormalizeCode("  ABCD-EFGH  ");

        Assert.Equal("ABCD-EFGH", result);
    }

    [Fact]
    public void NormalizeCode_RemovesSpaces()
    {
        var result = InvitationCodeGenerator.NormalizeCode("AB CD-EF GH");

        Assert.Equal("ABCD-EFGH", result);
    }

    [Fact]
    public void NormalizeCode_InsertsHyphenWhenMissing()
    {
        var result = InvitationCodeGenerator.NormalizeCode("ABCDEFGH");

        Assert.Equal("ABCD-EFGH", result);
    }

    [Fact]
    public void NormalizeCode_PreservesExistingHyphen()
    {
        var result = InvitationCodeGenerator.NormalizeCode("MINT-RUBY");

        Assert.Equal("MINT-RUBY", result);
    }

    // --- IsValidFormat ---

    [Theory]
    [InlineData("ABCD-EFGH")]
    [InlineData("MINT-RUBY")]
    [InlineData("FROG-AXLE")]
    public void IsValidFormat_AcceptsValidCodes(string code)
    {
        Assert.True(InvitationCodeGenerator.IsValidFormat(code));
    }

    [Fact]
    public void IsValidFormat_AcceptsLowercaseInput()
    {
        // IsValidFormat normalizes internally
        Assert.True(InvitationCodeGenerator.IsValidFormat("abcd-efgh"));
    }

    [Fact]
    public void IsValidFormat_AcceptsWithoutHyphenWhenEightChars()
    {
        Assert.True(InvitationCodeGenerator.IsValidFormat("ABCDEFGH"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC-DEFG")]  // 3-4 instead of 4-4
    [InlineData("ABCDE-FGH")] // 5-3
    [InlineData("AB-CD")]     // too short
    public void IsValidFormat_RejectsWrongLength(string code)
    {
        Assert.False(InvitationCodeGenerator.IsValidFormat(code));
    }

    [Fact]
    public void IsValidFormat_RejectsDigits()
    {
        Assert.False(InvitationCodeGenerator.IsValidFormat("ABC1-EFGH"));
        Assert.False(InvitationCodeGenerator.IsValidFormat("ABCD-3FGH"));
    }

    [Fact]
    public void IsValidFormat_RejectsSpecialCharacters()
    {
        Assert.False(InvitationCodeGenerator.IsValidFormat("ABC!-EFGH"));
        Assert.False(InvitationCodeGenerator.IsValidFormat("ABCD-EF@H"));
    }

    [Fact]
    public void IsValidFormat_RejectsNull()
    {
        Assert.False(InvitationCodeGenerator.IsValidFormat(null!));
    }

    [Fact]
    public void GenerateCode_AlwaysProducesValidFormat()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = InvitationCodeGenerator.GenerateCode();
            Assert.True(InvitationCodeGenerator.IsValidFormat(code),
                $"Generated code '{code}' failed format validation");
        }
    }
}
