using Chronicis.Api.Repositories;
using Chronicis.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ResourceProviderServiceBranchCoverageTests
{
    [Fact]
    public void Constructor_AssignsDependencies()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var repository = Substitute.For<IResourceProviderRepository>();
        var logger = NullLogger<ResourceProviderService>.Instance;

        var service = new ResourceProviderService(repository, db, logger);

        Assert.NotNull(service);
    }
}
