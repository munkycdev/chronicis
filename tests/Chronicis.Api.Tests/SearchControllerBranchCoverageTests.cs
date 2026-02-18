using Chronicis.Api.Controllers;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchControllerBranchCoverageTests
{
    [Fact]
    public void SearchController_ExtractSnippet_CoversBranches()
    {
        var extractSnippet = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SearchController), "ExtractSnippet");

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
