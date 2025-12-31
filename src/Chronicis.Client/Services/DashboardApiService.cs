using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Implementation of dashboard API service.
/// </summary>
public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<DashboardApiService> _logger;

    public DashboardApiService(HttpClient http, ILogger<DashboardApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<DashboardDto?> GetDashboardAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<DashboardDto>("api/dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard data");
            return null;
        }
    }
}
