namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Configuration model for Auth0 settings.
/// Values are populated from appsettings.json or environment variables.
/// </summary>
public class Auth0Configuration
{
    /// <summary>
    /// Auth0 tenant domain (e.g., "dev-chronicis.us.auth0.com")
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Auth0 API audience (e.g., "https://api.chronicis.app")
    /// This must match what's configured in Auth0 and what the client requests
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// Auth0 Client ID (from your Auth0 application settings)
    /// Used for reference; not directly used in API validation
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
