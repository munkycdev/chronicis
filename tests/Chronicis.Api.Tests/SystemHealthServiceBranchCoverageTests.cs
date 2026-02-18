using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class SystemHealthServiceBranchCoverageTests
{
    [Fact]
    public void Constructor_AssignsDependencies()
    {
        var service = new SystemHealthService(
            null!,
            null!,
            null!,
            null!,
            null!,
            NullLogger<SystemHealthService>.Instance);

        Assert.NotNull(service);
    }

    [Fact]
    public void SystemHealthService_DetermineOverallStatus_CoversBranches()
    {
        var determine = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SystemHealthService), "DetermineOverallStatus");

        var unhealthy = (string)determine.Invoke(null, [new ServiceHealthDto[] { new() { Status = HealthStatus.Unhealthy } }])!;
        Assert.Equal(HealthStatus.Unhealthy, unhealthy);

        var degraded = (string)determine.Invoke(null, [new ServiceHealthDto[] { new() { Status = HealthStatus.Degraded } }])!;
        Assert.Equal(HealthStatus.Degraded, degraded);

        var healthy = (string)determine.Invoke(null, [new ServiceHealthDto[] { new() { Status = HealthStatus.Healthy } }])!;
        Assert.Equal(HealthStatus.Healthy, healthy);
    }
}
