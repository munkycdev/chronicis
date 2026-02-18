using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Repositories;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class ResourceProviderRepositoryTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ResourceProviderRepository _repo;
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ResourceProviderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _repo = new ResourceProviderRepository(_context);
    }

    private bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
            _context.Dispose();
        _disposed = true;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private async Task<ResourceProvider> SeedProvider(
        string code, string name, bool isActive = true)
    {
        var provider = new ResourceProvider
        {
            Code = code,
            Name = name,
            Description = $"{name} description",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.ResourceProviders.Add(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    private async Task SeedWorldAssociation(
        Guid worldId, string providerCode, bool isEnabled)
    {
        _context.WorldResourceProviders.Add(new WorldResourceProvider
        {
            WorldId = worldId,
            ResourceProviderCode = providerCode,
            IsEnabled = isEnabled,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedByUserId = _userId
        });
        await _context.SaveChangesAsync();
    }

    // ── GetAllProvidersAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoProviders()
    {
        var result = await _repo.GetAllProvidersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveProviders()
    {
        await SeedProvider("srd14", "SRD 2014", isActive: true);
        await SeedProvider("old", "Legacy Pack", isActive: false);
        await SeedProvider("open5e", "Open5e", isActive: true);

        var result = await _repo.GetAllProvidersAsync();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, p => p.Code == "old");
    }

    [Fact]
    public async Task GetAll_ReturnsProvidersOrderedByName()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedProvider("open5e", "Open5e");
        await SeedProvider("ros", "Arcana Repository");

        var result = await _repo.GetAllProvidersAsync();

        Assert.Equal("Arcana Repository", result[0].Name);
        Assert.Equal("Open5e", result[1].Name);
        Assert.Equal("SRD 2014", result[2].Name);
    }

    // ── GetWorldProvidersAsync ───────────────────────────────────

    [Fact]
    public async Task GetWorld_DefaultsToDisabled_WhenNoAssociations()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedProvider("open5e", "Open5e");

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.False(r.IsEnabled));
    }

    [Fact]
    public async Task GetWorld_MarksEnabledProvider()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedProvider("open5e", "Open5e");
        await SeedWorldAssociation(_worldId, "srd14", isEnabled: true);

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        var srd = result.Single(r => r.Provider.Code == "srd14");
        var open5e = result.Single(r => r.Provider.Code == "open5e");
        Assert.True(srd.IsEnabled);
        Assert.False(open5e.IsEnabled);
    }

    [Fact]
    public async Task GetWorld_RespectsDisabledAssociation()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedWorldAssociation(_worldId, "srd14", isEnabled: false);

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        Assert.Single(result);
        Assert.False(result[0].IsEnabled);
    }

    [Fact]
    public async Task GetWorld_ExcludesInactiveProviders()
    {
        await SeedProvider("srd14", "SRD 2014", isActive: true);
        await SeedProvider("old", "Legacy", isActive: false);
        await SeedWorldAssociation(_worldId, "old", isEnabled: true);

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        Assert.Single(result);
        Assert.Equal("srd14", result[0].Provider.Code);
    }

    [Fact]
    public async Task GetWorld_ReturnsOrderedByName()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedProvider("open5e", "Open5e");
        await SeedProvider("ros", "Arcana Repository");

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        Assert.Equal("Arcana Repository", result[0].Provider.Name);
        Assert.Equal("Open5e", result[1].Provider.Name);
        Assert.Equal("SRD 2014", result[2].Provider.Name);
    }

    [Fact]
    public async Task GetWorld_IsolatesWorlds()
    {
        var otherWorldId = Guid.NewGuid();
        await SeedProvider("srd14", "SRD 2014");
        await SeedWorldAssociation(otherWorldId, "srd14", isEnabled: true);

        var result = await _repo.GetWorldProvidersAsync(_worldId);

        Assert.Single(result);
        Assert.False(result[0].IsEnabled);
    }

    // ── SetProviderEnabledAsync ──────────────────────────────────

    [Fact]
    public async Task Set_ReturnsFalse_WhenProviderCodeNotFound()
    {
        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "nonexistent", true, _userId);

        Assert.False(result);
    }

    [Fact]
    public async Task Set_ReturnsFalse_WhenProviderIsInactive()
    {
        await SeedProvider("old", "Legacy", isActive: false);

        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "old", true, _userId);

        Assert.False(result);
    }

    [Fact]
    public async Task Set_CreatesNewAssociation_WhenNoneExists()
    {
        await SeedProvider("srd14", "SRD 2014");

        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "srd14", true, _userId);

        Assert.True(result);
        var assoc = await _context.WorldResourceProviders
            .SingleAsync(w => w.WorldId == _worldId && w.ResourceProviderCode == "srd14");
        Assert.True(assoc.IsEnabled);
        Assert.Equal(_userId, assoc.ModifiedByUserId);
        Assert.NotNull(assoc.ModifiedAt);
    }

    [Fact]
    public async Task Set_CreatesDisabledAssociation_WhenNoneExists()
    {
        await SeedProvider("srd14", "SRD 2014");

        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "srd14", false, _userId);

        Assert.True(result);
        var assoc = await _context.WorldResourceProviders
            .SingleAsync(w => w.WorldId == _worldId && w.ResourceProviderCode == "srd14");
        Assert.False(assoc.IsEnabled);
    }

    [Fact]
    public async Task Set_UpdatesExistingAssociation_ToEnabled()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedWorldAssociation(_worldId, "srd14", isEnabled: false);

        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "srd14", true, _userId);

        Assert.True(result);
        var assoc = await _context.WorldResourceProviders
            .SingleAsync(w => w.WorldId == _worldId && w.ResourceProviderCode == "srd14");
        Assert.True(assoc.IsEnabled);
    }

    [Fact]
    public async Task Set_UpdatesExistingAssociation_ToDisabled()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedWorldAssociation(_worldId, "srd14", isEnabled: true);

        var result = await _repo.SetProviderEnabledAsync(
            _worldId, "srd14", false, _userId);

        Assert.True(result);
        var assoc = await _context.WorldResourceProviders
            .SingleAsync(w => w.WorldId == _worldId && w.ResourceProviderCode == "srd14");
        Assert.False(assoc.IsEnabled);
    }

    [Fact]
    public async Task Set_UpdatesAuditFields_OnExistingAssociation()
    {
        await SeedProvider("srd14", "SRD 2014");
        await SeedWorldAssociation(_worldId, "srd14", isEnabled: false);

        var newUserId = Guid.NewGuid();
        var beforeUpdate = DateTimeOffset.UtcNow;

        await _repo.SetProviderEnabledAsync(
            _worldId, "srd14", true, newUserId);

        var assoc = await _context.WorldResourceProviders
            .SingleAsync(w => w.WorldId == _worldId && w.ResourceProviderCode == "srd14");
        Assert.Equal(newUserId, assoc.ModifiedByUserId);
        Assert.True(assoc.ModifiedAt >= beforeUpdate);
    }
}
