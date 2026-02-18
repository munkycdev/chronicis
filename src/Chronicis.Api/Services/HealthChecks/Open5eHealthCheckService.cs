using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

[ExcludeFromCodeCoverage]
public class Open5eHealthCheckService : HealthCheckServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public Open5eHealthCheckService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Open5eHealthCheckService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override async Task<(string Status, string? Message)> PerformHealthCheckAsync()
    {
        var baseUrl = _configuration["ExternalLinks:Open5e:BaseUrl"] ?? "https://api.open5e.com";

        try
        {
            // Try a lightweight endpoint
            var testUrl = $"{baseUrl}/v2/monsters/?limit=1";
            var response = await _httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                return (HealthStatus.Healthy, "Open5e API accessible");
            }
            else
            {
                return (HealthStatus.Degraded, $"Open5e API returned {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (HealthStatus.Unhealthy, $"Open5e API error: {ex.Message}");
        }
    }
}
