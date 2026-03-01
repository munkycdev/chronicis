using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldLinkServiceTests
{
    [Fact]
    public async Task GetWorldLinksAsync_CoversNotFoundAndSuccess()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        db.WorldLinks.AddRange(
            new WorldLink { Id = Guid.NewGuid(), WorldId = world.Id, Title = "Zeta", Url = "https://zeta.example", CreatedAt = DateTime.UtcNow },
            new WorldLink { Id = Guid.NewGuid(), WorldId = world.Id, Title = "Alpha", Url = "https://alpha.example", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = new WorldLinkService(db);

        var notFound = await sut.GetWorldLinksAsync(world.Id, Guid.NewGuid());
        Assert.Equal(ServiceStatus.NotFound, notFound.Status);

        var success = await sut.GetWorldLinksAsync(world.Id, ownerId);
        Assert.Equal(ServiceStatus.Success, success.Status);
        Assert.Equal(2, success.Value!.Count);
        Assert.Equal("Alpha", success.Value[0].Title);
        Assert.Equal("Zeta", success.Value[1].Title);
    }

    [Fact]
    public async Task CreateWorldLinkAsync_CoversNotFoundAndSuccess()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        await db.SaveChangesAsync();

        var sut = new WorldLinkService(db);
        var dto = new WorldLinkCreateDto
        {
            Url = " https://example.com ",
            Title = " Example ",
            Description = " Desc "
        };

        var notFound = await sut.CreateWorldLinkAsync(world.Id, dto, Guid.NewGuid());
        Assert.Equal(ServiceStatus.NotFound, notFound.Status);

        var success = await sut.CreateWorldLinkAsync(world.Id, dto, ownerId);
        Assert.Equal(ServiceStatus.Success, success.Status);
        Assert.Equal("https://example.com", success.Value!.Url);
        Assert.Equal("Example", success.Value.Title);
        Assert.Equal("Desc", success.Value.Description);
    }

    [Fact]
    public async Task UpdateWorldLinkAsync_CoversAllBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        var existing = new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            Url = "https://old.example",
            Title = "Old",
            CreatedAt = DateTime.UtcNow
        };
        db.WorldLinks.Add(existing);
        await db.SaveChangesAsync();

        var sut = new WorldLinkService(db);
        var dto = new WorldLinkUpdateDto
        {
            Url = " https://new.example ",
            Title = " New ",
            Description = "   "
        };

        var worldNotFound = await sut.UpdateWorldLinkAsync(world.Id, existing.Id, dto, Guid.NewGuid());
        Assert.Equal(ServiceStatus.NotFound, worldNotFound.Status);

        var linkNotFound = await sut.UpdateWorldLinkAsync(world.Id, Guid.NewGuid(), dto, ownerId);
        Assert.Equal(ServiceStatus.NotFound, linkNotFound.Status);

        var success = await sut.UpdateWorldLinkAsync(world.Id, existing.Id, dto, ownerId);
        Assert.Equal(ServiceStatus.Success, success.Status);
        Assert.Equal("https://new.example", success.Value!.Url);
        Assert.Equal("New", success.Value.Title);
        Assert.Null(success.Value.Description);
    }

    [Fact]
    public async Task DeleteWorldLinkAsync_CoversAllBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        var existing = new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            Url = "https://delete.example",
            Title = "Delete",
            CreatedAt = DateTime.UtcNow
        };
        db.WorldLinks.Add(existing);
        await db.SaveChangesAsync();

        var sut = new WorldLinkService(db);

        var worldNotFound = await sut.DeleteWorldLinkAsync(world.Id, existing.Id, Guid.NewGuid());
        Assert.Equal(ServiceStatus.NotFound, worldNotFound.Status);

        var linkNotFound = await sut.DeleteWorldLinkAsync(world.Id, Guid.NewGuid(), ownerId);
        Assert.Equal(ServiceStatus.NotFound, linkNotFound.Status);

        var success = await sut.DeleteWorldLinkAsync(world.Id, existing.Id, ownerId);
        Assert.Equal(ServiceStatus.Success, success.Status);
        Assert.False(db.WorldLinks.Any(wl => wl.Id == existing.Id));
    }
}
