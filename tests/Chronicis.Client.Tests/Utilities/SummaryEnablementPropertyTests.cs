// Feature: handwritten-session-notes, Property 6: Summary Enablement Based on Body Content
using Chronicis.Client.Utilities;
using FsCheck;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

/// <summary>
/// Property 6: Summary Enablement Based on Body Content
/// Generate random nullable strings (null, empty, whitespace, content); verify AI summary
/// enabled iff Body is non-null with at least one non-whitespace char.
///
/// Validates: Requirements 7.1, 7.3
/// </summary>
public class SummaryEnablementPropertyTests
{
    [Fact]
    public void Null_Body_Returns_Disabled()
    {
        // **Validates: Requirements 7.1, 7.3**
        var result = SummaryEnablementState.IsSummaryEnabled(null);
        Assert.False(result);
    }

    [Fact]
    public void Empty_Body_Returns_Disabled()
    {
        // **Validates: Requirements 7.1, 7.3**
        var result = SummaryEnablementState.IsSummaryEnabled("");
        Assert.False(result);
    }

    [Fact]
    public void Whitespace_Only_Body_Returns_Disabled()
    {
        // **Validates: Requirements 7.1, 7.3**
        var whitespaceChars = new[] { ' ', '\t', '\n', '\r', '\v', '\f' };
        var whitespaceGen = Gen.Elements(whitespaceChars)
            .Select(c => c.ToString())
            .ArrayOf()
            .Select(arr => string.Concat(arr))
            .Where(s => s.Length > 0);

        Prop.ForAll(Arb.From(whitespaceGen), body =>
        {
            return !SummaryEnablementState.IsSummaryEnabled(body);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void NonWhitespace_Body_Returns_Enabled()
    {
        // **Validates: Requirements 7.1, 7.3**
        Prop.ForAll(Arb.From<NonEmptyString>(), nes =>
        {
            var body = nes.Get;
            // Filter to strings that have at least one non-whitespace char
            return string.IsNullOrWhiteSpace(body) || SummaryEnablementState.IsSummaryEnabled(body);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Enabled_Iff_Not_NullOrWhiteSpace_For_Any_String()
    {
        // **Validates: Requirements 7.1, 7.3**
        Prop.ForAll(Arb.From<string?>(), body =>
        {
            var expected = !string.IsNullOrWhiteSpace(body);
            var actual = SummaryEnablementState.IsSummaryEnabled(body);
            return expected == actual;
        }).QuickCheckThrowOnFailure();
    }
}
