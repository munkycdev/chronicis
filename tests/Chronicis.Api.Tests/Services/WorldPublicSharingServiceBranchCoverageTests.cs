using Chronicis.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldPublicSharingServiceBranchCoverageTests
{
    [Fact]
    public void WorldPublicSharingService_ValidateAndSlugHelpers_CoverBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var service = new WorldPublicSharingService(db, NullLogger<WorldPublicSharingService>.Instance);

        Assert.Equal("Public slug is required", service.ValidatePublicSlug(" "));
        Assert.Equal("Public slug must be at least 3 characters", service.ValidatePublicSlug("ab"));
        Assert.Equal("Public slug must be 100 characters or less", service.ValidatePublicSlug(new string('a', 101)));
        Assert.Contains("lowercase letters", service.ValidatePublicSlug("Bad_Slug"));
        Assert.Equal("This slug is reserved and cannot be used", service.ValidatePublicSlug("api"));
        Assert.Null(service.ValidatePublicSlug("good-slug"));

        var generateSuggestedSlug = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(WorldPublicSharingService), "GenerateSuggestedSlug");
        var suggested = (string)generateSuggestedSlug.Invoke(null, ["__A__"])!;
        Assert.Equal("a00", suggested);
    }
}
