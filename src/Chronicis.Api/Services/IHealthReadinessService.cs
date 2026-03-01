namespace Chronicis.Api.Services;

public interface IHealthReadinessService
{
    Task<HealthReadinessResult> GetReadinessAsync();
}

public class HealthReadinessResult
{
    public bool IsHealthy { get; set; }
    public string DatabaseStatus { get; set; } = "unknown";
}

