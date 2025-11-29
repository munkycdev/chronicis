using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Functions;

public abstract class BaseAuthenticatedFunction
{
    protected readonly IUserService _userService;
    protected readonly ILogger _logger;
    protected readonly Auth0Configuration _auth0Config;

    protected BaseAuthenticatedFunction(
        IUserService userService,
        IOptions<Auth0Configuration> auth0Config,
        ILogger logger)
    {
        _userService = userService;
        _auth0Config = auth0Config.Value;
        _logger = logger;
    }

    protected async Task<(User? user, HttpResponseData? errorResponse)> AuthenticateRequestAsync(HttpRequestData req)
    {
        _logger.LogInformation("DEBUG: Auth0 Config - Domain: {Domain}, Audience: {Audience}",
    _auth0Config.Domain, _auth0Config.Audience);

        // Extract and validate user info from JWT token
        var principal = await Auth0AuthenticationHelper.GetUserFromTokenAsync(
            req,
            _auth0Config.Domain,
            _auth0Config.Audience);

        if (principal == null)
        {
            _logger.LogWarning("Authentication failed: No valid token found");
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteStringAsync("Authentication required. Please provide a valid Auth0 token.");
            return (null, response);
        }

        try
        {
            // Get or create user in our database
            var user = await _userService.GetOrCreateUserAsync(
                principal.Auth0UserId,
                principal.Email,
                principal.DisplayName,
                principal.AvatarUrl
            );

            _logger.LogInformation("Authenticated user {UserId} ({Email})", user.Id, user.Email);

            return (user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user with Auth0 ID: {Auth0UserId}", principal.Auth0UserId);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("An error occurred during authentication.");
            return (null, response);
        }
    }

    protected async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}