using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

[ExcludeFromCodeCoverage]
public class SystemHealthStatusDto
{
    public DateTime Timestamp { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public List<ServiceHealthDto> Services { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public class ServiceHealthDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ResponseTimeMs { get; set; }
    public string? Message { get; set; }
    public string ServiceKey { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

[ExcludeFromCodeCoverage]
public static class HealthStatus
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unhealthy = "unhealthy";
}

[ExcludeFromCodeCoverage]
public static class ServiceKeys
{
    public const string Api = "api";
    public const string Database = "database";
    public const string AzureOpenAI = "azure-openai";
    public const string BlobStorage = "blob-storage";
    public const string Auth0 = "auth0";
    public const string Open5e = "open5e";
}
