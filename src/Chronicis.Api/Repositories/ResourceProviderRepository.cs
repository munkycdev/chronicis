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
    public async Task<List<(ResourceProvider Provider, bool IsEnabled)>> GetWorldProvidersAsync(Guid worldId)
    {
        // Get all active providers
        var providers = await _context.ResourceProviders
            .Where(rp => rp.IsActive)
            .OrderBy(rp => rp.Name)
            .AsNoTracking()
            .ToListAsync();

        // Get enabled providers for this world
        var enabledProviderCodes = await _context.WorldResourceProviders
            .Where(wrp => wrp.WorldId == worldId && wrp.IsEnabled)
            .Select(wrp => wrp.ResourceProviderCode)
            .ToListAsync();

        // Combine into result
        return providers.Select(p => (
            Provider: p,
            IsEnabled: enabledProviderCodes.Contains(p.Code)
        )).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> SetProviderEnabledAsync(Guid worldId, string providerCode, bool enabled, Guid userId)
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
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedByUserId = userId
            };
            _context.WorldResourceProviders.Add(association);
        }
        else
        {
            // Update existing association
            association.IsEnabled = enabled;
            association.ModifiedAt = DateTimeOffset.UtcNow;
            association.ModifiedByUserId = userId;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
