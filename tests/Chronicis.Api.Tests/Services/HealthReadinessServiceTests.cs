using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests;

public class HealthReadinessServiceTests
{
    [Fact]
    public async Task GetReadinessAsync_WhenDatabaseIsAvailable_ReturnsHealthy()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new HealthReadinessService(db);

        var readiness = await sut.GetReadinessAsync();

        Assert.True(readiness.IsHealthy);
        Assert.Equal("connected", readiness.DatabaseStatus);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenDatabaseIsUnavailable_ReturnsUnhealthy()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=chronicis_unavailable;User Id=sa;Password=invalid;TrustServerCertificate=True;Connection Timeout=1")
            .Options;

        using var db = new ChronicisDbContext(options);
        var sut = new HealthReadinessService(db);

        var readiness = await sut.GetReadinessAsync();

        Assert.False(readiness.IsHealthy);
        Assert.Equal("unavailable", readiness.DatabaseStatus);
    }
}
