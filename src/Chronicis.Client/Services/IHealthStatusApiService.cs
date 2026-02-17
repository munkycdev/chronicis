using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IHealthStatusApiService
{
    Task<SystemHealthStatusDto?> GetSystemHealthAsync();
}

public class HealthStatusApiService : IHealthStatusApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HealthStatusApiService> _logger;

    public HealthStatusApiService(HttpClient httpClient, ILogger<HealthStatusApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SystemHealthStatusDto?> GetSystemHealthAsync()
    {
        try
        {
            _logger.LogInformation("Requesting health status from /health/status");
            var response = await _httpClient.GetAsync("/health/status");

            _logger.LogInformation("Health status API returned {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Health status response: {Content}", content);

                return System.Text.Json.JsonSerializer.Deserialize<SystemHealthStatusDto>(content, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Health status API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error while fetching system health status");
            return null;
        }
        catch (TaskCanceledException tcEx)
        {
            _logger.LogError(tcEx, "Timeout while fetching system health status");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch system health status");
            return null;
        }
    }
}
