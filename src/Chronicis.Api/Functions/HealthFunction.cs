using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]  // Public endpoint - no auth required
    [Function("Health")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        var healthCheck = new HealthCheckResponse
        {
            Status = "Healthy",
            Message = "API is healthy!",
            Timestamp = DateTime.UtcNow
        };

        await response.WriteAsJsonAsync(healthCheck);

        return response;
    }
}
