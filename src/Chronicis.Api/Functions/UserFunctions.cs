using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for user profile management.
/// </summary>
public class UserFunctions
{
    private readonly IUserService _userService;
    private readonly ILogger<UserFunctions> _logger;

    public UserFunctions(IUserService userService, ILogger<UserFunctions> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's profile information.
    /// </summary>
    [Function("GetUserProfile")]
    public async Task<HttpResponseData> GetUserProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var profile = await _userService.GetUserProfileAsync(user.Id);
        if (profile == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(profile);
        return response;
    }

    /// <summary>
    /// Marks the current user's onboarding as complete.
    /// </summary>
    [Function("CompleteOnboarding")]
    public async Task<HttpResponseData> CompleteOnboarding(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/me/complete-onboarding")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var success = await _userService.CompleteOnboardingAsync(user.Id);
        if (!success)
        {
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Failed to complete onboarding");
            return error;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { success = true });
        return response;
    }
}
