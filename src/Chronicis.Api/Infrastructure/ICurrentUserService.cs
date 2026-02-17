using Chronicis.Shared.Models;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Service for accessing the current authenticated user.
/// Replaces the FunctionContext-based user access pattern from Azure Functions.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user, or null if not authenticated.
    /// </summary>
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Gets the current authenticated user.
    /// Throws InvalidOperationException if not authenticated.
    /// </summary>
    Task<User> GetRequiredUserAsync();

    /// <summary>
    /// Gets the Auth0 user ID from the current claims, or null if not authenticated.
    /// </summary>
    string? GetAuth0UserId();

    /// <summary>
    /// Gets whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
