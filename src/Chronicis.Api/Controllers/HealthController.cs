using Chronicis.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for health checks.
/// These endpoints do NOT require authentication.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ChronicisDbContext context,
        ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/health - Basic health check endpoint.
    /// Returns 200 OK if the API is running.
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// GET /api/health/ready - Readiness check including database connectivity.
    /// Returns 200 OK if the API and database are ready.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();

            if (!canConnect)
            {
                _logger.LogWarning("Health check failed: Cannot connect to database");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    checks = new
                    {
                        database = "unavailable"
                    }
                });
            }

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "connected"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
