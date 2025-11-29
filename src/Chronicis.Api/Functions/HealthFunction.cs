using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Chronicis.Api.Functions;

public class HealthFunction : BaseAuthenticatedFunction
{
    public HealthFunction(
            ILogger<HealthFunction> logger,
            IUserService userService,
            IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger) { }

    [Function("Health")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check endpoint called");

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
