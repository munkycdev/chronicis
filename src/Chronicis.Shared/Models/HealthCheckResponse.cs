using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

[ExcludeFromCodeCoverage]
public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public string Message { get; set; } = "API is healthy!";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
