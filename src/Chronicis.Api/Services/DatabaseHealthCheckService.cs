using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class DatabaseHealthCheckService : HealthCheckServiceBase
{
    private readonly ChronicisDbContext _context;

    public DatabaseHealthCheckService(ChronicisDbContext context, ILogger<DatabaseHealthCheckService> logger)
        : base(logger)
    {
        _context = context;
    }

    protected override async Task<(string Status, string? Message)> PerformHealthCheckAsync()
    {
        var canConnect = await _context.Database.CanConnectAsync();

        if (!canConnect)
        {
            return (HealthStatus.Unhealthy, "Cannot connect to database");
        }

        // Optional: Try a simple query to verify deeper connectivity
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            return (HealthStatus.Healthy, "Database connection successful");
        }
        catch (Exception ex)
        {
            return (HealthStatus.Degraded, $"Connected but query failed: {ex.Message}");
        }
    }
}
