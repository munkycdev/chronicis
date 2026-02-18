using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

[ExcludeFromCodeCoverage]
public class Auth0HealthCheckService : HealthCheckServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public Auth0HealthCheckService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Auth0HealthCheckService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override async Task<(string Status, string? Message)> PerformHealthCheckAsync()
    {
        var domain = _configuration["Auth0:Domain"];

        if (string.IsNullOrEmpty(domain))
        {
            return (HealthStatus.Unhealthy, "Auth0 domain not configured");
        }

        try
        {
            var wellKnownUrl = $"https://{domain}/.well-known/openid-configuration";
            var response = await _httpClient.GetAsync(wellKnownUrl);

            if (response.IsSuccessStatusCode)
            {
                return (HealthStatus.Healthy, "Auth0 well-known endpoint accessible");
            }
            else
            {
                return (HealthStatus.Degraded, $"Auth0 returned {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (HealthStatus.Unhealthy, $"Auth0 connectivity error: {ex.Message}");
        }
    }
}
