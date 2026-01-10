using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Helper class to extract and validate user information from Auth0 JWT tokens
/// in Azure Functions HTTP requests.
/// </summary>
public static class Auth0AuthenticationHelper
{
    private static ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;

    /// <summary>
    /// Extracts and validates user claims from the Authorization header JWT token.
    /// </summary>
    public static UserPrincipal? GetUserFromTokenAsync(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData req,
        string auth0Domain,
        string auth0Audience,
        out string error)
    {
        error = "";

        // Check for custom header first (used to bypass Azure SWA's auth interception)
        // Azure SWA intercepts and replaces the standard Authorization header with its own token
        string? token = null;
        
        if (req.Headers.TryGetValues("X-Auth0-Token", out var customTokenValues))
        {
            token = customTokenValues.FirstOrDefault();
            Console.WriteLine($"Auth0 Token: {token}");
        }

        if (string.IsNullOrEmpty(token))
        {
            error = "Token value is empty";
            return null;
        }

        try
        {
            var claimsPrincipal = ValidateToken(token, auth0Domain, auth0Audience, out error);
            if (claimsPrincipal == null)
            {
                return null;
            }

            // DEBUG: Log all claims to help diagnose missing user info
            Console.WriteLine("=== JWT Claims ===");
            foreach (var claim in claimsPrincipal.Claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }
            Console.WriteLine("==================");

            var auth0UserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0UserId))
            {
                error = "auth0UserId is null";
                return null;
            }

            const string customNamespace = "https://chronicis.app";

            var email = claimsPrincipal.FindFirst($"{customNamespace}/email")?.Value
                       ?? claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value
                       ?? claimsPrincipal.FindFirst("email")?.Value
                       ?? "";

            var displayName = claimsPrincipal.FindFirst($"{customNamespace}/name")?.Value
                             ?? claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value
                             ?? claimsPrincipal.FindFirst("name")?.Value
                             ?? claimsPrincipal.FindFirst("nickname")?.Value
                             ?? claimsPrincipal.FindFirst("preferred_username")?.Value
                             ?? claimsPrincipal.FindFirst("given_name")?.Value
                             ?? ExtractNameFromEmail(email)
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
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}";
            return null;
        }
    }

    private static ClaimsPrincipal? ValidateToken(
        string token,
        string auth0Domain,
        string auth0Audience,
        out string error)
    {
        error = "";
        
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (_configurationManager == null)
            {
                var issuer = $"https://{auth0Domain}/";
                _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{issuer}.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());
            }

            var config = _configurationManager.GetConfigurationAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = $"https://{auth0Domain}/",
                ValidAudiences = new[] { auth0Audience },
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                TryAllIssuerSigningKeys = true
            };

            var result = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return result;
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            error = $"KeyNotFound: {ex.Message}, Token: {token}";
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            error = $"ValidationError: {ex.Message}";
            return null;
        }
        catch (Exception ex)
        {
            error = $"Error: {ex.Message}";
            return null;
        }
    }

    private static string PadBase64(string base64)
    {
        var output = base64.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: return output + "==";
            case 3: return output + "=";
            default: return output;
        }
    }

    /// <summary>
    /// Extracts a display name from an email address as a fallback.
    /// e.g., "john.doe@example.com" becomes "John Doe"
    /// </summary>
    private static string? ExtractNameFromEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return null;

        var localPart = email.Split('@')[0];
        
        // Replace common separators with spaces
        var name = localPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ');

        // Title case each word
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleCased = words.Select(w => 
            char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1).ToLower() : ""));

        var result = string.Join(" ", titleCased);
        
        // Don't return if it looks like gibberish (all numbers, too short, etc.)
        if (result.Length < 2 || result.All(c => char.IsDigit(c)))
            return null;

        return result;
    }
}

/// <summary>
/// Represents the user principal extracted from an Auth0 JWT token.
/// </summary>
public class UserPrincipal
{
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
