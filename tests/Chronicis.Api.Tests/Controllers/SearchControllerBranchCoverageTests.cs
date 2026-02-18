using System.Text.RegularExpressions;
using Chronicis.Api.Controllers;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchControllerBranchCoverageTests
{
    [Fact]
    public void SearchController_HashtagPattern_StaticInitializer_IsCovered()
    {
        var field = typeof(SearchController).GetField(
            "HashtagPattern",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(field);

        var regex = Assert.IsType<Regex>(field!.GetValue(null));
        var match = regex.Match("value #tag_1 value");
        Assert.True(match.Success);
        Assert.Equal("tag_1", match.Groups[1].Value);
    }

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
