using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing user accounts
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets an existing user or creates a new one based on Auth0 user ID.
    /// This method is called on every authenticated request to ensure the user exists in our database.
    /// On first login, also creates a default World for the user.
    /// </summary>
    /// <param name="auth0UserId">The Auth0 user ID (e.g., "google-oauth2|123456")</param>
    /// <param name="email">User's email from Auth0</param>
    /// <param name="displayName">User's display name from Auth0</param>
    /// <param name="avatarUrl">User's avatar URL from Auth0 (optional)</param>
    /// <returns>The user entity from our database</returns>
    Task<User> GetOrCreateUserAsync(string auth0UserId, string email, string displayName, string? avatarUrl);

    /// <summary>
    /// Gets a user by their internal database ID
    /// </summary>
    /// <param name="userId">Internal user ID</param>
    /// <returns>User entity or null if not found</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Updates the user's last login timestamp
    /// </summary>
    /// <param name="userId">Internal user ID</param>
    Task UpdateLastLoginAsync(Guid userId);
}
