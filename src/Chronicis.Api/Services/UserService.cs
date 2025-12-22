using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
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
    private readonly IWorldService _worldService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ChronicisDbContext context, 
        IWorldService worldService,
        ILogger<UserService> logger)
    {
        _context = context;
        _worldService = worldService;
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
            user = new User
            {
                Id = Guid.NewGuid(),
                Auth0UserId = auth0UserId,
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user {UserId} for Auth0 ID {Auth0UserId}", user.Id, auth0UserId);

            // Create default world for new user
            await CreateDefaultWorldAsync(user.Id);
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
            user.LastLoginAt = DateTime.UtcNow;
            needsUpdate = true;

            if (needsUpdate)
            {
                await _context.SaveChangesAsync();
            }
        }

        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Creates a default world with root structure for a new user
    /// </summary>
    private async Task CreateDefaultWorldAsync(Guid userId)
    {
        _logger.LogInformation("Creating default world for user {UserId}", userId);

        var createDto = new WorldCreateDto
        {
            Name = "My World",
            Description = "Your personal world for campaigns and adventures"
        };

        await _worldService.CreateWorldAsync(createDto, userId);
    }
}
