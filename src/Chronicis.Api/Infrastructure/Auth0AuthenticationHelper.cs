using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        if (!req.Headers.TryGetValues("Authorization", out var authHeaderValues))
        {
            error = "No Authorization header";
            return null;
        }

        var authHeader = authHeaderValues.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            error = "Auth header doesn't start with Bearer";
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

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
            
            // Log raw token info for debugging
            var parts = token.Split('.');
            error = $"Token length: {token.Length}, Parts: {parts.Length}";
            
            // Log first 20 chars of token
            error += $", Token start: {token.Substring(0, Math.Min(50, token.Length))}";
            
            if (parts.Length >= 1)
            {
                try 
                {
                    var headerJson = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(PadBase64(parts[0])));
                    error += $", Raw header: {headerJson}";
                }
                catch (Exception ex)
                {
                    error += $", Header decode error: {ex.Message}";
                }
            }
            
            var jwtToken = handler.ReadJwtToken(token);
            error += $", Parsed alg: {jwtToken.Header.Alg}, kid: {jwtToken.Header.Kid ?? "NULL"}";

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
            
            error += $", JWKS keys count: {config.SigningKeys.Count()}";
            error += $", Keys: [{string.Join(", ", config.SigningKeys.Select(k => k.KeyId))}]";

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
            error = "Validation succeeded";
            return result;
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            error += $", KeyNotFound: {ex.Message}";
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            error += $", ValidationError: {ex.Message}";
            return null;
        }
        catch (Exception ex)
        {
            error += $", Error: {ex.Message}";
            return null;
        }
    }

    private static string PadBase64(string base64)
    {
        // JWT uses base64url encoding, convert to standard base64
        var output = base64.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: return output + "==";
            case 3: return output + "=";
            default: return output;
        }
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
