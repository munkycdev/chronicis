using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
// Don't add: using Microsoft.Azure.Functions.Worker.Http; here

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Helper class to extract and validate user information from Auth0 JWT tokens
/// in Azure Functions HTTP requests.
/// </summary>
public static class Auth0AuthenticationHelper
{
    private static ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;

    /// <summary>
    /// Extracts and validates user claims from the Authorization header JWT token
    /// </summary>
    /// <param name="req">HTTP request with Authorization header</param>
    /// <param name="auth0Domain">Auth0 domain (e.g., "dev-chronicis.us.auth0.com")</param>
    /// <param name="auth0Audience">Auth0 audience (e.g., "https://api.chronicis.app")</param>
    /// <returns>User principal with Auth0 claims, or null if not authenticated or invalid</returns>
    public static async Task<UserPrincipal?> GetUserFromTokenAsync(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData req,  // ? Full namespace
        string auth0Domain,
        string auth0Audience)
    {
        // DEBUG: Log all headers
        Console.WriteLine("=== DEBUG: Headers ===");
        foreach (var header in req.Headers)
        {
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        // Get Authorization header
        if (!req.Headers.TryGetValues("Authorization", out var authHeaderValues))
        {
            Console.WriteLine("DEBUG: No Authorization header found");
            return null;
        }

        var authHeader = authHeaderValues.FirstOrDefault();
        Console.WriteLine($"DEBUG: Authorization header = {authHeader?.Substring(0, Math.Min(50, authHeader?.Length ?? 0))}...");

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("DEBUG: Authorization header is empty or doesn't start with 'Bearer'");
            return null;
        }

        // Extract token
        var token = authHeader.Substring("Bearer ".Length).Trim();
        Console.WriteLine($"DEBUG: Token length = {token.Length}");

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("DEBUG: Token is empty after extraction");
            return null;
        }

        try
        {
            Console.WriteLine($"DEBUG: Attempting to validate token with domain={auth0Domain}, audience={auth0Audience}");

            // Validate the token
            var claimsPrincipal = await ValidateTokenAsync(token, auth0Domain, auth0Audience);
            if (claimsPrincipal == null)
            {
                return null;
            }

            // Extract Auth0 user ID (sub claim)
            var auth0UserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0UserId))
            {
                return null;
            }

            // Extract other claims (with custom namespace from Auth0 Action if present)
            const string customNamespace = "https://chronicis.app";

            var email = claimsPrincipal.FindFirst($"{customNamespace}/email")?.Value
                       ?? claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value
                       ?? claimsPrincipal.FindFirst("email")?.Value
                       ?? "";

            var displayName = claimsPrincipal.FindFirst($"{customNamespace}/name")?.Value
                             ?? claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value
                             ?? claimsPrincipal.FindFirst("name")?.Value
                             ?? claimsPrincipal.FindFirst("nickname")?.Value
                             ?? "Unknown User";

            var avatarUrl = claimsPrincipal.FindFirst($"{customNamespace}/picture")?.Value
                           ?? claimsPrincipal.FindFirst("picture")?.Value;

            return new UserPrincipal
            {
                Auth0UserId = auth0UserId,
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<ClaimsPrincipal?> ValidateTokenAsync(
        string token,
        string auth0Domain,
        string auth0Audience)
    {
        // Initialize configuration manager if needed
        if (_configurationManager == null)
        {
            var issuer = $"https://{auth0Domain}/";
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{issuer}.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        }

        var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://{auth0Domain}/",
            ValidAudiences = new[] { auth0Audience },
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return result;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the user principal extracted from an Auth0 JWT token
/// </summary>
public class UserPrincipal
{
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}