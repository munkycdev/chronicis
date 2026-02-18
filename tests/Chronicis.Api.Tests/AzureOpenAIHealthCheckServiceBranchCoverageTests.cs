using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class AzureOpenAIHealthCheckServiceBranchCoverageTests
{
    [Fact]
    public async Task AzureOpenAIHealthCheckService_PerformHealthCheck_CoversBranches()
    {
        var method = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(AzureOpenAIHealthCheckService), "PerformHealthCheckAsync");

        async Task<(string Status, string? Message)> InvokeAsync(Dictionary<string, string?> values)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            using var httpClient = new HttpClient();
            var service = new AzureOpenAIHealthCheckService(config, httpClient, NullLogger<AzureOpenAIHealthCheckService>.Instance);
            return await (Task<(string Status, string? Message)>)method.Invoke(service, [])!;
        }

        var r1 = await InvokeAsync(new Dictionary<string, string?>());
        Assert.Equal(HealthStatus.Unhealthy, r1.Status);

        var r2 = await InvokeAsync(new Dictionary<string, string?> { ["AzureOpenAI:Endpoint"] = "https://example.test" });
        Assert.Equal(HealthStatus.Unhealthy, r2.Status);

        var r3 = await InvokeAsync(new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://example.test",
            ["AzureOpenAI:ApiKey"] = "key"
        });
        Assert.Equal(HealthStatus.Unhealthy, r3.Status);

        var r4 = await InvokeAsync(new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "::::",
            ["AzureOpenAI:ApiKey"] = "key",
            ["AzureOpenAI:DeploymentName"] = "dep"
        });
        Assert.Equal(HealthStatus.Unhealthy, r4.Status);

        var r5 = await InvokeAsync(new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://example.test",
            ["AzureOpenAI:ApiKey"] = "key",
            ["AzureOpenAI:DeploymentName"] = "dep"
        });
        Assert.Equal(HealthStatus.Healthy, r5.Status);
    }
}
