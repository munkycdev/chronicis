using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class AutocompleteCommitDecisionTests
{
    // ── SelectExisting ───────────────────────────────────────────────

    [Fact]
    public void Decide_SuggestionsExistAndValidIndex_ReturnsSelectExisting()
    {
        var result = AutocompleteCommitDecision.Decide("wat", 3, 1, false, false);

        var select = Assert.IsType<AutocompleteCommitDecision.SelectExisting>(result);
        Assert.Equal(1, select.Index);
    }

    [Fact]
    public void Decide_SuggestionsExistAndIndexZero_ReturnsSelectExisting()
    {
        var result = AutocompleteCommitDecision.Decide("wat", 1, 0, false, false);

        var select = Assert.IsType<AutocompleteCommitDecision.SelectExisting>(result);
        Assert.Equal(0, select.Index);
    }

    // ── CreateNew ────────────────────────────────────────────────────

    [Fact]
    public void Decide_NoSuggestionsInternalQueryThreeChars_ReturnsCreateNew()
    {
        var result = AutocompleteCommitDecision.Decide("war", 0, 0, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("War", create.Name);
    }

    [Fact]
    public void Decide_NoSuggestionsInternalQueryLong_CapitalizesFirstLetter()
    {
        var result = AutocompleteCommitDecision.Decide("waterdeep castle", 0, 0, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("Waterdeep castle", create.Name);
    }

    [Fact]
    public void Decide_NoSuggestionsQueryAlreadyCapitalized_KeepsCapitalization()
    {
        var result = AutocompleteCommitDecision.Decide("Waterdeep", 0, 0, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("Waterdeep", create.Name);
    }

    [Fact]
    public void Decide_QueryWithSlashSegments_NameIsLastSegment()
    {
        var result = AutocompleteCommitDecision.Decide("places/castle ward", 0, 0, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("Castle ward", create.Name);
    }

    // ── DoNothing ────────────────────────────────────────────────────

    [Fact]
    public void Decide_NoSuggestionsExternalQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide("fireball", 0, 0, true, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NoSuggestionsMapQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide("waterdeep", 0, 0, false, true);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NoSuggestionsEmptyQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide(string.Empty, 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NullQuery_ReturnsDoNothing()
    {
        // Exercises the null-guard branch in (query ?? string.Empty)
        var result = AutocompleteCommitDecision.Decide(null!, 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NoSuggestionsWhitespaceQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide("   ", 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NoSuggestionsTwoCharQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide("wa", 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    [Fact]
    public void Decide_NoSuggestionsOneCharQuery_ReturnsDoNothing()
    {
        var result = AutocompleteCommitDecision.Decide("w", 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }

    // ── Edge: suggestions exist but index out of range ───────────────

    [Fact]
    public void Decide_SuggestionsExistButIndexNegative_FallsThroughToCreateNew()
    {
        // selectedIndex -1 fails the >= 0 guard → falls through to CreateNew
        var result = AutocompleteCommitDecision.Decide("war", 3, -1, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("War", create.Name);
    }

    [Fact]
    public void Decide_SuggestionsExistButIndexAtCount_FallsThroughToCreateNew()
    {
        // selectedIndex == suggestionsCount fails the < guard → falls through to CreateNew
        var result = AutocompleteCommitDecision.Decide("war", 2, 2, false, false);

        var create = Assert.IsType<AutocompleteCommitDecision.CreateNew>(result);
        Assert.Equal("War", create.Name);
    }

    // ── Edge: empty last segment after slash ─────────────────────────

    [Fact]
    public void Decide_QueryEndsWithSlash_ReturnsDoNothing()
    {
        // "places/" → last segment is "" → ExtractArticleName returns "" → DoNothing
        var result = AutocompleteCommitDecision.Decide("places/", 0, 0, false, false);

        Assert.IsType<AutocompleteCommitDecision.DoNothing>(result);
    }
}
