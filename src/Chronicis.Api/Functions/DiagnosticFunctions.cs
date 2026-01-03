using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Diagnostic functions for troubleshooting production issues
/// </summary>
public class DiagnosticFunctions
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiagnosticFunctions> _logger;

    public DiagnosticFunctions(
        IConfiguration configuration,
        ILogger<DiagnosticFunctions> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Check blob storage configuration
    /// GET /api/diagnostic/blob-config
    /// </summary>
    [Function("DiagnosticBlobConfig")]
    public HttpResponseData CheckBlobConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostic/blob-config")]
        HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} checking blob config", user.Id);

        try
        {
            var connectionString = _configuration["BlobStorage:ConnectionString"];
            var containerName = _configuration["BlobStorage:ContainerName"];

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($@"
Blob Storage Configuration:
- ConnectionString: {(string.IsNullOrEmpty(connectionString) ? "MISSING" : "PRESENT (length: " + connectionString.Length + ")")}
- ContainerName: {(string.IsNullOrEmpty(containerName) ? "MISSING (will use default)" : containerName)}

Environment Variables:
- BlobStorage__ConnectionString: {(string.IsNullOrEmpty(_configuration["BlobStorage__ConnectionString"]) ? "MISSING" : "PRESENT")}
- BlobStorage__ContainerName: {(string.IsNullOrEmpty(_configuration["BlobStorage__ContainerName"]) ? "MISSING" : "PRESENT")}
            ");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blob config");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            error.WriteString($"Error: {ex.Message}");
            return error;
        }
    }
}
