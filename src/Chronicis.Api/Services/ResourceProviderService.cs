using Chronicis.Api.Data;
using Chronicis.Shared.Extensions;
using Chronicis.Api.Repositories;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service implementation for managing resource providers with authorization.
/// </summary>
public class ResourceProviderService : IResourceProviderService
{
    private readonly IResourceProviderRepository _repository;
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ResourceProviderService> _logger;

    public ResourceProviderService(
        IResourceProviderRepository repository,
        ChronicisDbContext context,
        ILogger<ResourceProviderService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ResourceProvider>> GetAllProvidersAsync()
    {
        return await _repository.GetAllProvidersAsync();
    }

    /// <inheritdoc/>
    public async Task<List<(ResourceProvider Provider, bool IsEnabled)>> GetWorldProvidersAsync(Guid worldId, Guid userId)
    {
        // Verify world exists and user has access
        var world = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
        {
            _logger.LogWarning("World {WorldId} not found", worldId);
            throw new KeyNotFoundException($"World {worldId} not found");
        }

        // Check if user is owner or member
        var isOwner = world.OwnerId == userId;
        var isMember = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == userId);

        if (!isOwner && !isMember)
        {
            _logger.LogWarning("User {UserId} unauthorized to access world {WorldId}", userId, worldId);
            throw new UnauthorizedAccessException($"User does not have access to world {worldId}");
        }

        return await _repository.GetWorldProvidersAsync(worldId);
    }

    /// <inheritdoc/>
    public async Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId)
    {
        // Verify world exists
        var world = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
        {
            _logger.LogWarning("World {WorldId} not found", worldId);
            throw new KeyNotFoundException($"World {worldId} not found");
        }

        // Check if user is the owner (only owners can modify settings)
        if (world.OwnerId != userId)
        {
            _logger.LogWarning("User {UserId} is not owner of world {WorldId}", userId, worldId);
            throw new UnauthorizedAccessException($"Only the world owner can modify resource provider settings");
        }

        // Attempt to enable/disable the provider
        var result = await _repository.SetProviderEnabledAsync(worldId, providerCode, enabled, userId);

        if (!result)
        {
            _logger.LogWarning("Provider {ProviderCode} not found or inactive", providerCode);
            throw new KeyNotFoundException($"Resource provider '{providerCode}' not found or inactive");
        }

        _logger.LogDebugSanitized(
            "User {UserId} {Action} provider {ProviderCode} for world {WorldId}",
            userId,
            enabled ? "enabled" : "disabled",
            providerCode,
            worldId);

        return true;
    }
}
