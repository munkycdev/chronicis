using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Chronicis.Client.Services;

/// <summary>
/// User information retrieved from Auth0
/// </summary>
public class UserInfo
{
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Service for managing authentication state and user information
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets the current authenticated user's information
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync();

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    Task<bool> IsAuthenticatedAsync();
}

/// <summary>
/// Implementation of authentication service using ASP.NET Core authentication
/// </summary>
public class AuthService : IAuthService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private UserInfo? _cachedUser;

    public AuthService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        if (_cachedUser != null)
        {
            return _cachedUser;
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        const string customNamespace = "https://chronicis.app";

        // Extract claims from Auth0 token
        var auth0UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? "";

        var email = user.FindFirst($"{customNamespace}/email")?.Value
                   ?? user.FindFirst(ClaimTypes.Email)?.Value
                   ?? user.FindFirst("email")?.Value
                   ?? "";

        var displayName = user.FindFirst($"{customNamespace}/name")?.Value
                         ?? user.FindFirst(ClaimTypes.Name)?.Value
                         ?? user.FindFirst("name")?.Value
                         ?? user.FindFirst("preferred_username")?.Value
                         ?? "Unknown User";

        var avatarUrl = user.FindFirst($"{customNamespace}/picture")?.Value
                       ?? user.FindFirst("picture")?.Value;

        _cachedUser = new UserInfo
        {
            Auth0UserId = auth0UserId,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl
        };

        return _cachedUser;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }
}
