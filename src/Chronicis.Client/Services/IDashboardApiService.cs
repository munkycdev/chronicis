using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for dashboard data operations.
/// </summary>
public interface IDashboardApiService
{
    /// <summary>
    /// Gets aggregated dashboard data for the current user.
    /// </summary>
    Task<DashboardDto?> GetDashboardAsync();
}
