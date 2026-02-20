using Bunit;
using Chronicis.Client.Pages.Admin;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Client.Tests.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages.Admin;

public class StatusTests : MudBlazorTestContext
{
    [Fact]
    public void Status_WhileAuthorizing_ShowsProgress()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();
        var authTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        adminAuth.IsSysAdminAsync().Returns(authTcs.Task);
        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Status_Unauthorized_ShowsPermissionError()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();

        adminAuth.IsSysAdminAsync().Returns(false);
        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();

        Assert.Contains("do not have permission", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Status_Authorized_Healthy_RendersSummary()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();

        adminAuth.IsSysAdminAsync().Returns(true);
        api.GetSystemHealthAsync().Returns(new Chronicis.Shared.DTOs.SystemHealthStatusDto
        {
            OverallStatus = Chronicis.Shared.DTOs.HealthStatus.Healthy,
            Services =
            [
                new Chronicis.Shared.DTOs.ServiceHealthDto
                {
                    ServiceKey = Chronicis.Shared.DTOs.ServiceKeys.Api,
                    Status = Chronicis.Shared.DTOs.HealthStatus.Healthy,
                    ResponseTimeMs = 10,
                    CheckedAt = DateTime.UtcNow
                },
                new Chronicis.Shared.DTOs.ServiceHealthDto
                {
                    ServiceKey = "other",
                    Status = "unknown",
                    Message = "details",
                    ResponseTimeMs = 42,
                    CheckedAt = DateTime.UtcNow
                }
            ]
        });

        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("System is HEALTHY", cut.Markup);
            Assert.Contains("API", cut.Markup);
            Assert.Contains("details", cut.Markup);
        });
    }

    [Fact]
    public void Status_Authorized_ApiFailure_ShowsError()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();

        adminAuth.IsSysAdminAsync().Returns(true);
        api.GetSystemHealthAsync().Returns(Task.FromException<Chronicis.Shared.DTOs.SystemHealthStatusDto?>(new Exception("boom")));

        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Unable to fetch system status", cut.Markup));
    }

    [Fact]
    public void Status_Refresh_RequestsHealthAgain()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();

        adminAuth.IsSysAdminAsync().Returns(true);
        api.GetSystemHealthAsync().Returns(new Chronicis.Shared.DTOs.SystemHealthStatusDto
        {
            OverallStatus = "unknown",
            Services =
            [
                new Chronicis.Shared.DTOs.ServiceHealthDto
                {
                    ServiceKey = "other",
                    Status = "unknown",
                    ResponseTimeMs = 1,
                    CheckedAt = DateTime.UtcNow
                }
            ]
        });

        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();
        cut.WaitForState(() => cut.Markup.Contains("System is UNKNOWN", StringComparison.Ordinal));

        cut.Find("button").Click();

        api.Received(2).GetSystemHealthAsync();
    }

    [Theory]
    [InlineData(HealthStatus.Degraded, ServiceKeys.Database, HealthStatus.Degraded, "Database", "DEGRADED")]
    [InlineData(HealthStatus.Unhealthy, ServiceKeys.AzureOpenAI, HealthStatus.Unhealthy, "AI", "UNHEALTHY")]
    [InlineData("unknown", ServiceKeys.BlobStorage, "unknown", "Storage", "UNKNOWN")]
    [InlineData("unknown", ServiceKeys.Auth0, "unknown", "Auth", "UNKNOWN")]
    [InlineData("unknown", ServiceKeys.Open5e, "unknown", "External Data", "UNKNOWN")]
    public void Status_Authorized_ServiceMappings_RenderExpectedValues(
        string overallStatus,
        string serviceKey,
        string serviceStatus,
        string expectedServiceName,
        string expectedStatusText)
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var api = Substitute.For<IHealthStatusApiService>();

        adminAuth.IsSysAdminAsync().Returns(true);
        api.GetSystemHealthAsync().Returns(new SystemHealthStatusDto
        {
            OverallStatus = overallStatus,
            Services =
            [
                new ServiceHealthDto
                {
                    ServiceKey = serviceKey,
                    Status = serviceStatus,
                    ResponseTimeMs = 10,
                    CheckedAt = DateTime.UtcNow
                }
            ]
        });

        Services.AddSingleton(adminAuth);
        Services.AddSingleton(api);

        var cut = RenderComponent<Status>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains($"System is {overallStatus.ToUpperInvariant()}", cut.Markup);
            Assert.Contains(expectedServiceName, cut.Markup);
            Assert.Contains(expectedStatusText, cut.Markup);
        });
    }
}
