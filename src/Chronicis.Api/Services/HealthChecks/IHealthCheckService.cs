using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IHealthCheckService
{
    Task<ServiceHealthDto> CheckHealthAsync(string serviceName, string serviceKey);
}
