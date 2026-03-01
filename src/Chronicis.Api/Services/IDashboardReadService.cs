using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IDashboardReadService
{
    Task<DashboardDto> GetDashboardAsync(Guid userId, string userDisplayName);
}

