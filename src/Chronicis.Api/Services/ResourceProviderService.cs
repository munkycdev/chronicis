using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Api.Repositories;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service implementation for managing resource providers with authorization.
/// </summary>
public class ResourceProviderService : IResourceProviderService
{
    private static readonly Regex LookupKeyPattern = new("^[a-z0-9][a-z0-9_-]{0,49}$", RegexOptions.Compiled);

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
    public async Task<List<(ResourceProvider Provider, bool IsEnabled, string LookupKey)>> GetWorldProvidersAsync(Guid worldId, Guid userId)
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
    public async Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId, string? lookupKey = null)
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

        var providerExists = await _context.ResourceProviders
            .AsNoTracking()
            .AnyAsync(rp => rp.Code == providerCode && rp.IsActive);

        if (!providerExists)
        {
            _logger.LogWarning("Provider {ProviderCode} not found or inactive", providerCode);
            throw new KeyNotFoundException($"Resource provider '{providerCode}' not found or inactive");
        }

        var lookupKeyUpdateRequested = lookupKey != null;
        var normalizedLookupKey = lookupKeyUpdateRequested
            ? NormalizeLookupKey(lookupKey)
            : null;

        if (lookupKeyUpdateRequested && normalizedLookupKey != null && !LookupKeyPattern.IsMatch(normalizedLookupKey))
        {
            throw new ArgumentException(
                "Lookup key must start with a letter/number and use only lowercase letters, numbers, '-' or '_', max 50 characters.",
                nameof(lookupKey));
        }

        var worldProviders = await _repository.GetWorldProvidersAsync(worldId);
        var targetProvider = worldProviders.FirstOrDefault(
            p => p.Provider.Code.Equals(providerCode, StringComparison.OrdinalIgnoreCase));

        var effectiveLookupKey = lookupKeyUpdateRequested
            ? normalizedLookupKey ?? providerCode.ToLowerInvariant()
            : targetProvider == default
                ? providerCode.ToLowerInvariant()
                : targetProvider.LookupKey.ToLowerInvariant();

        if (enabled)
        {
            var keyConflict = worldProviders.Any(p =>
                p.IsEnabled
                && !p.Provider.Code.Equals(providerCode, StringComparison.OrdinalIgnoreCase)
                && p.LookupKey.Equals(effectiveLookupKey, StringComparison.OrdinalIgnoreCase));

            if (keyConflict)
            {
                throw new InvalidOperationException(
                    $"Lookup key '{effectiveLookupKey}' is already in use by another enabled provider in this world.");
            }
        }

        // Attempt to enable/disable the provider
        var result = await _repository.SetProviderEnabledAsync(
            worldId,
            providerCode,
            enabled,
            userId,
            lookupKeyUpdateRequested ? lookupKey : null);

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

    private static string? NormalizeLookupKey(string? lookupKey)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
        {
            return null;
        }

        return lookupKey.Trim().ToLowerInvariant();
    }
}
