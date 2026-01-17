using Chronicis.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    private readonly IConfiguration _configuration;

    public HealthController(
        ChronicisDbContext context,
        ILogger<HealthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /api/health - Basic health check endpoint.
    /// Returns 200 OK if the API is running.
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        _logger.LogInformation("Health Endpoint Called");
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

            // Get connection string info for diagnostics (mask password)
            var connStr = _configuration.GetConnectionString("ChronicisDb") ?? "";
            var maskedConnStr = MaskConnectionString(connStr);


            _logger.LogInformation("Readiness endpoint succeeded");

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "connected",
                    connectionInfo = maskedConnStr
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

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "(empty)";

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var hasPassword = !string.IsNullOrEmpty(builder.Password);
            var hasUserId = !string.IsNullOrEmpty(builder.UserID);
            
            return $"Server={builder.DataSource}; Database={builder.InitialCatalog}; " +
                   $"User={(!hasUserId ? "(none)" : "****")}; Password={(!hasPassword ? "(none)" : "****")}; " +
                   $"MARS={builder.MultipleActiveResultSets}; Encrypt={builder.Encrypt}";
        }
        catch
        {
            return "(invalid connection string format)";
        }
    }
}
