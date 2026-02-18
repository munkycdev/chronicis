using Chronicis.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class AutoLinkServiceBranchCoverageTests
{
    [Fact]
    public void AutoLinkService_PrivateHelpers_CoverRemainingBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var service = new AutoLinkService(db, NullLogger<AutoLinkService>.Instance);

        var getProtectedRanges = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(AutoLinkService), "GetProtectedRanges");
        var isInProtectedRange = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(AutoLinkService), "IsInProtectedRange");

        var body = "<p>tag</p><span data-type=\"wiki-link\">a</span><span data-type=\"external-link\">b</span> [[12345678-1234-1234-1234-123456789012]]";
        var ranges = ((IEnumerable<(int Start, int End)>)getProtectedRanges.Invoke(service, [body])!).ToList();
        Assert.NotEmpty(ranges);

        var overlaps = (bool)isInProtectedRange.Invoke(service, [1, 1, ranges])!;
        var nonOverlaps = (bool)isInProtectedRange.Invoke(service, [body.Length + 10, 2, ranges])!;
        Assert.True(overlaps);
        Assert.False(nonOverlaps);
    }
}
