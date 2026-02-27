using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface ISystemHealthService
{
    Task<SystemHealthStatusDto> GetSystemHealthAsync();
}

public class SystemHealthService : ISystemHealthService
{
    private readonly DatabaseHealthCheckService _databaseHealth;
    private readonly AzureOpenAIHealthCheckService _azureOpenAIHealth;
    private readonly BlobStorageHealthCheckService _blobStorageHealth;
    private readonly Auth0HealthCheckService _auth0Health;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(
        DatabaseHealthCheckService databaseHealth,
        AzureOpenAIHealthCheckService azureOpenAIHealth,
        BlobStorageHealthCheckService blobStorageHealth,
        Auth0HealthCheckService auth0Health,
        ILogger<SystemHealthService> logger)
    {
        _databaseHealth = databaseHealth;
        _azureOpenAIHealth = azureOpenAIHealth;
        _blobStorageHealth = blobStorageHealth;
        _auth0Health = auth0Health;
        _logger = logger;
    }

    public async Task<SystemHealthStatusDto> GetSystemHealthAsync()
    {
        var timestamp = DateTime.UtcNow;
        _logger.LogInformation("Starting system health check");

        // Run all health checks in parallel
        var healthCheckTasks = new[]
        {
            _databaseHealth.CheckHealthAsync("Database", ServiceKeys.Database),
            _azureOpenAIHealth.CheckHealthAsync("Azure OpenAI", ServiceKeys.AzureOpenAI),
            _blobStorageHealth.CheckHealthAsync("Document Storage", ServiceKeys.BlobStorage),
            _auth0Health.CheckHealthAsync("Auth0", ServiceKeys.Auth0)
        };

        // Add API self-check
        var apiHealthTask = Task.FromResult(new ServiceHealthDto
        {
            Name = "API",
            ServiceKey = ServiceKeys.Api,
            Status = HealthStatus.Healthy,
            Message = "API is responding",
            ResponseTimeMs = 0,
            CheckedAt = timestamp
        });

        var allTasks = healthCheckTasks.Concat(new[] { apiHealthTask }).ToArray();
        var results = await Task.WhenAll(allTasks);

        // Determine overall status
        var overallStatus = DetermineOverallStatus(results);

        _logger.LogInformation("System health check completed. Overall status: {Status}", overallStatus);

        return new SystemHealthStatusDto
        {
            Timestamp = timestamp,
            OverallStatus = overallStatus,
            Services = results.ToList()
        };
    }

    private static string DetermineOverallStatus(ServiceHealthDto[] services)
    {
        var hasUnhealthy = services.Any(s => s.Status == HealthStatus.Unhealthy);
        var hasDegraded = services.Any(s => s.Status == HealthStatus.Degraded);

        if (hasUnhealthy)
            return HealthStatus.Unhealthy;

        if (hasDegraded)
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }
}
