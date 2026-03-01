using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchControllerBranchCoverageTests
{
    [Fact]
    public void SearchReadService_CleanForDisplay_CoversMarkupAndWikiLinkBranches()
    {
        var cleanForDisplay = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SearchReadService), "CleanForDisplay");

        var input = "<p>alpha</p> [[00000000-0000-0000-0000-000000000001|beta]]   gamma";
        var cleaned = (string)cleanForDisplay.Invoke(null, [input])!;

        Assert.Equal("alpha beta gamma", cleaned);
    }

    [Fact]
    public void SearchReadService_ExtractSnippet_CoversBranches()
    {
        var extractSnippet = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SearchReadService), "ExtractSnippet");

        Assert.Equal(string.Empty, (string)extractSnippet.Invoke(null, ["", "q", 3])!);

        var notFoundShort = (string)extractSnippet.Invoke(null, ["abcdef", "xyz", 10])!;
        Assert.Equal("abcdef", notFoundShort);

        var notFoundLong = (string)extractSnippet.Invoke(null, [new string('a', 30), "xyz", 5])!;
        Assert.EndsWith("...", notFoundLong);

        var foundMiddle = (string)extractSnippet.Invoke(null, ["prefix target suffix", "target", 3])!;
        Assert.StartsWith("...", foundMiddle);
        Assert.EndsWith("...", foundMiddle);
    }
}
