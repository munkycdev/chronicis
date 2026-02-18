using Chronicis.Api.Controllers;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticlesControllerBranchCoverageTests
{
    [Fact]
    public void ArticlesController_ParseAliases_CoversBranches()
    {
        var parse = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ArticlesController), "ParseAliases");

        var empty = (List<string>)parse.Invoke(null, [null])!;
        Assert.Empty(empty);

        var parsed = (List<string>)parse.Invoke(null, ["  one, two,One,   ," + new string('x', 201)])!;
        Assert.Equal(2, parsed.Count);
        Assert.Contains("one", parsed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("two", parsed, StringComparer.OrdinalIgnoreCase);
    }
}
