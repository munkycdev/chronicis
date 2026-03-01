using Chronicis.Api.Data;

namespace Chronicis.Api.Services;

public class HealthReadinessService : IHealthReadinessService
{
    private readonly ChronicisDbContext _context;

    public HealthReadinessService(ChronicisDbContext context)
    {
        _context = context;
    }

    public async Task<HealthReadinessResult> GetReadinessAsync()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        return new HealthReadinessResult
        {
            IsHealthy = canConnect,
            DatabaseStatus = canConnect ? "connected" : "unavailable"
        };
    }
}

