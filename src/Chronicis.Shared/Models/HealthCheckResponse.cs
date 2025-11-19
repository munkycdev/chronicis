namespace Chronicis.Shared.Models;

public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public string Message { get; set; } = "API is healthy!";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
