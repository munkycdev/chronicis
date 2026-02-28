using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Repositories;

/// <summary>
/// Repository implementation for managing resource providers and their world associations.
/// </summary>
public class ResourceProviderRepository : IResourceProviderRepository
{
    private readonly ChronicisDbContext _context;

    public ResourceProviderRepository(ChronicisDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<ResourceProvider>> GetAllProvidersAsync()
    {
        return await _context.ResourceProviders
            .Where(rp => rp.IsActive)
            .OrderBy(rp => rp.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<(ResourceProvider Provider, bool IsEnabled, string LookupKey)>> GetWorldProvidersAsync(Guid worldId)
    {
        // Get all active providers
        var providers = await _context.ResourceProviders
            .Where(rp => rp.IsActive)
            .OrderBy(rp => rp.Name)
            .AsNoTracking()
            .ToListAsync();

        // Get world-level provider associations (enabled state + optional lookup key override)
        var worldAssociations = await _context.WorldResourceProviders
            .Where(wrp => wrp.WorldId == worldId)
            .AsNoTracking()
            .ToDictionaryAsync(
                wrp => wrp.ResourceProviderCode,
                wrp => wrp,
                StringComparer.OrdinalIgnoreCase);

        // Combine into result
        return providers.Select(p => (
            Provider: p,
            IsEnabled: worldAssociations.TryGetValue(p.Code, out var association) && association.IsEnabled,
            LookupKey: worldAssociations.TryGetValue(p.Code, out var wrp) && !string.IsNullOrWhiteSpace(wrp.LookupKey)
                ? wrp.LookupKey!
                : p.Code
        )).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId, string? lookupKey = null)
    {
        // Verify provider exists
        var providerExists = await _context.ResourceProviders
            .AnyAsync(rp => rp.Code == providerCode && rp.IsActive);

        if (!providerExists)
        {
            return false;
        }

        // Find or create the association
        var association = await _context.WorldResourceProviders
            .FirstOrDefaultAsync(wrp => wrp.WorldId == worldId && wrp.ResourceProviderCode == providerCode);

        if (association == null)
        {
            // Create new association
            association = new WorldResourceProvider
            {
                WorldId = worldId,
                ResourceProviderCode = providerCode,
                IsEnabled = enabled,
                LookupKey = NormalizeLookupKeyForStorage(lookupKey),
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedByUserId = userId
            };
            _context.WorldResourceProviders.Add(association);
        }
        else
        {
            // Update existing association
            association.IsEnabled = enabled;
            if (lookupKey != null)
            {
                association.LookupKey = NormalizeLookupKeyForStorage(lookupKey);
            }

            association.ModifiedAt = DateTimeOffset.UtcNow;
            association.ModifiedByUserId = userId;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private static string? NormalizeLookupKeyForStorage(string? lookupKey)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
        {
            return null;
        }

        return lookupKey.Trim().ToLowerInvariant();
    }
}
