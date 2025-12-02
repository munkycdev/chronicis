using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Implementation of user management service
/// </summary>
public class UserService : IUserService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ChronicisDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(
        string auth0UserId,
        string email,
        string displayName,
        string? avatarUrl)
    {
        // Try to find existing user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);

        if (user == null)
        {
            // Create new user
            _logger.LogInformation("Creating new user for Auth0 ID: {Auth0UserId}", auth0UserId);

            user = new User
            {
                Auth0UserId = auth0UserId,
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                CreatedDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user {UserId} for Auth0 ID: {Auth0UserId}",
                user.Id, auth0UserId);
        }
        else
        {
            // Update user info in case it changed (e.g., user changed their name/avatar in Auth0)
            bool needsUpdate = false;

            if (user.Email != email)
            {
                user.Email = email;
                needsUpdate = true;
            }

            if (user.DisplayName != displayName)
            {
                user.DisplayName = displayName;
                needsUpdate = true;
            }

            if (user.AvatarUrl != avatarUrl)
            {
                user.AvatarUrl = avatarUrl;
                needsUpdate = true;
            }

            // Always update last login
            user.LastLoginDate = DateTime.UtcNow;
            needsUpdate = true;

            if (needsUpdate)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated user {UserId} info", user.Id);
            }
        }

        return user;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
