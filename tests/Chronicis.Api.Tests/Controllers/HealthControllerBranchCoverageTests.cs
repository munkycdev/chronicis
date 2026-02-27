using Chronicis.Api.Controllers;
using Xunit;

namespace Chronicis.Api.Tests;

public class HealthControllerBranchCoverageTests
{
    [Fact]
    public void HealthController_MaskConnectionString_CoversBranches()
    {
        var mask = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(HealthController), "MaskConnectionString");

        Assert.Equal("(empty)", (string)mask.Invoke(null, [null])!);
        Assert.Equal("(empty)", (string)mask.Invoke(null, [""])!);

        var masked = (string)mask.Invoke(null, ["Server=.;Database=Db;User Id=sa;Password=secret;MultipleActiveResultSets=True;Encrypt=True"])!;
        Assert.Contains("User=****", masked);
        Assert.Contains("Password=****", masked);

        var maskedNoCreds = (string)mask.Invoke(null, ["Server=.;Database=Db;"])!;
        Assert.Contains("User=(none)", maskedNoCreds);
        Assert.Contains("Password=(none)", maskedNoCreds);

        Assert.Equal("(invalid connection string format)", (string)mask.Invoke(null, ["not-a-conn-string;"])!);
    }
}
