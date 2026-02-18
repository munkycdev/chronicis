using System.Diagnostics;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public abstract class HealthCheckServiceBase : IHealthCheckService
{
    private readonly ILogger _logger;

    protected HealthCheckServiceBase(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ServiceHealthDto> CheckHealthAsync(string serviceName, string serviceKey)
    {
        var stopwatch = Stopwatch.StartNew();
        var checkedAt = DateTime.UtcNow;

        try
        {
            var (status, message) = await PerformHealthCheckAsync();
            stopwatch.Stop();

            return new ServiceHealthDto
            {
                Name = serviceName,
                ServiceKey = serviceKey,
                Status = status,
                Message = message,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                CheckedAt = checkedAt
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check failed for {ServiceName}", serviceName);

            return new ServiceHealthDto
            {
                Name = serviceName,
                ServiceKey = serviceKey,
                Status = HealthStatus.Unhealthy,
                Message = ex.Message,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                CheckedAt = checkedAt
            };
        }
    }

    protected abstract Task<(string Status, string? Message)> PerformHealthCheckAsync();
}
