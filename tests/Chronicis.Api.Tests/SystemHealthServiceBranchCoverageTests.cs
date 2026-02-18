using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Api.Tests;

public class SystemHealthServiceBranchCoverageTests
{
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
