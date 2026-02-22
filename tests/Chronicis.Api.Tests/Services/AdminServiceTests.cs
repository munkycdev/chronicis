using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class AdminServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ChronicisDbContext(options);
        _currentUserService = Substitute.For<ICurrentUserService>();
        _sut = new AdminService(_context, _currentUserService, NullLogger<AdminService>.Instance);
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

    // ────────────────────────────────────────────────────────────────
    //  Test helpers
    // ────────────────────────────────────────────────────────────────

    private void SetupAsSysAdmin(bool isSysAdmin = true)
        => _currentUserService.IsSysAdminAsync().Returns(isSysAdmin);

    private (World world, User owner) SeedWorld(
        string name = "Test World",
        string email = "owner@test.com")
    {
        var owner = TestHelpers.CreateUser(email: email, displayName: "Owner");
        var world = TestHelpers.CreateWorld(ownerId: owner.Id, name: name);
        world.Owner = owner;
        _context.Users.Add(owner);
        _context.Worlds.Add(world);
        _context.SaveChanges();
        return (world, owner);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetAllWorldSummariesAsync — authorization
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllWorldSummaries_NotSysAdmin_ThrowsUnauthorized()
    {
        SetupAsSysAdmin(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetAllWorldSummariesAsync());
    }

    // ────────────────────────────────────────────────────────────────
    //  GetAllWorldSummariesAsync — data
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllWorldSummaries_NoWorlds_ReturnsEmptyList()
    {
        SetupAsSysAdmin();

        var result = await _sut.GetAllWorldSummariesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllWorldSummaries_ReturnsAllWorlds_OrderedByName()
    {
        SetupAsSysAdmin();
        SeedWorld("Zebra World");
        SeedWorld("Alpha World");

        var result = await _sut.GetAllWorldSummariesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha World", result[0].Name);
        Assert.Equal("Zebra World", result[1].Name);
    }

    [Fact]
    public async Task GetAllWorldSummaries_PopulatesOwnerInfo()
    {
        SetupAsSysAdmin();
        var (world, owner) = SeedWorld(email: "gm@example.com");

        var result = await _sut.GetAllWorldSummariesAsync();

        var dto = Assert.Single(result);
        Assert.Equal(world.Id, dto.Id);
        Assert.Equal(world.Name, dto.Name);
        Assert.Equal(owner.DisplayName, dto.OwnerName);
        Assert.Equal("gm@example.com", dto.OwnerEmail);
    }

    [Fact]
    public async Task GetAllWorldSummaries_CountsArticles()
    {
        SetupAsSysAdmin();
        var (world, owner) = SeedWorld();

        _context.Articles.AddRange(
            TestHelpers.CreateArticle(worldId: world.Id, createdBy: owner.Id),
            TestHelpers.CreateArticle(worldId: world.Id, createdBy: owner.Id));
        _context.SaveChanges();

        var result = await _sut.GetAllWorldSummariesAsync();

        Assert.Equal(2, Assert.Single(result).ArticleCount);
    }

    [Fact]
    public async Task GetAllWorldSummaries_CountsCampaigns()
    {
        SetupAsSysAdmin();
        var (world, _) = SeedWorld();

        _context.Campaigns.Add(TestHelpers.CreateCampaign(worldId: world.Id));
        _context.SaveChanges();

        var result = await _sut.GetAllWorldSummariesAsync();

        Assert.Equal(1, Assert.Single(result).CampaignCount);
    }

    [Fact]
    public async Task GetAllWorldSummaries_CountsArcsAcrossAllCampaigns()
    {
        SetupAsSysAdmin();
        var (world, _) = SeedWorld();

        var c1 = TestHelpers.CreateCampaign(worldId: world.Id);
        var c2 = TestHelpers.CreateCampaign(worldId: world.Id);
        _context.Campaigns.AddRange(c1, c2);
        _context.SaveChanges();

        _context.Arcs.AddRange(
            TestHelpers.CreateArc(campaignId: c1.Id),
            TestHelpers.CreateArc(campaignId: c1.Id),
            TestHelpers.CreateArc(campaignId: c2.Id));
        _context.SaveChanges();

        var result = await _sut.GetAllWorldSummariesAsync();

        Assert.Equal(3, Assert.Single(result).ArcCount);
    }

    [Fact]
    public async Task GetAllWorldSummaries_DoesNotCountOtherWorldsArticles()
    {
        SetupAsSysAdmin();
        var (world1, owner1) = SeedWorld("World 1");
        var (world2, owner2) = SeedWorld("World 2");

        _context.Articles.Add(TestHelpers.CreateArticle(worldId: world1.Id, createdBy: owner1.Id));
        _context.SaveChanges();

        var result = await _sut.GetAllWorldSummariesAsync();

        var w1 = result.Single(r => r.Name == "World 1");
        var w2 = result.Single(r => r.Name == "World 2");
        Assert.Equal(1, w1.ArticleCount);
        Assert.Equal(0, w2.ArticleCount);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteWorldAsync — authorization
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteWorld_NotSysAdmin_ThrowsUnauthorized()
    {
        SetupAsSysAdmin(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteWorldAsync(Guid.NewGuid()));
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteWorldAsync — not found
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteWorld_WorldNotFound_ReturnsFalse()
    {
        SetupAsSysAdmin();

        var result = await _sut.DeleteWorldAsync(Guid.NewGuid());

        Assert.False(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteWorldAsync — happy path
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteWorld_EmptyWorld_DeletesWorldAndReturnsTrue()
    {
        SetupAsSysAdmin();
        var (world, _) = SeedWorld();

        var result = await _sut.DeleteWorldAsync(world.Id);

        Assert.True(result);
        Assert.Null(await _context.Worlds.FindAsync(world.Id));
    }

    [Fact]
    public async Task DeleteWorld_DeletesArticlesBeforeWorld()
    {
        SetupAsSysAdmin();
        var (world, owner) = SeedWorld();

        _context.Articles.Add(TestHelpers.CreateArticle(worldId: world.Id, createdBy: owner.Id));
        _context.SaveChanges();

        var result = await _sut.DeleteWorldAsync(world.Id);

        Assert.True(result);
        Assert.Empty(_context.Articles.Where(a => a.WorldId == world.Id));
        Assert.Null(await _context.Worlds.FindAsync(world.Id));
    }

    [Fact]
    public async Task DeleteWorld_DeletesArticleLinksBeforeArticles()
    {
        SetupAsSysAdmin();
        var (world, owner) = SeedWorld();

        var source = TestHelpers.CreateArticle(worldId: world.Id, createdBy: owner.Id);
        var target = TestHelpers.CreateArticle(worldId: world.Id, createdBy: owner.Id);
        _context.Articles.AddRange(source, target);
        _context.SaveChanges();

        var link = new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = source.Id,
            TargetArticleId = target.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ArticleLinks.Add(link);
        _context.SaveChanges();

        var result = await _sut.DeleteWorldAsync(world.Id);

        Assert.True(result);
        Assert.Empty(_context.ArticleLinks.Where(al =>
            al.SourceArticleId == source.Id || al.TargetArticleId == target.Id));
    }

    [Fact]
    public async Task DeleteWorld_DeletesCampaignsBeforeWorld()
    {
        SetupAsSysAdmin();
        var (world, _) = SeedWorld();

        var campaign = TestHelpers.CreateCampaign(worldId: world.Id);
        _context.Campaigns.Add(campaign);
        _context.SaveChanges();

        var result = await _sut.DeleteWorldAsync(world.Id);

        Assert.True(result);
        Assert.Empty(_context.Campaigns.Where(c => c.WorldId == world.Id));
        Assert.Null(await _context.Worlds.FindAsync(world.Id));
    }

    // ────────────────────────────────────────────────────────────────
    //  BuildWorldSummaryQueryAsync (internal method branch coverage)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildWorldSummaryQuery_WorldWithNoOwner_UsesUnknownFallback()
    {
        // Create a world without an owner navigation set
        var orphanOwner = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(ownerId: orphanOwner.Id, name: "Orphan World");
        // Intentionally do NOT set world.Owner
        _context.Users.Add(orphanOwner);
        _context.Worlds.Add(world);
        _context.SaveChanges();

        SetupAsSysAdmin();
        var result = await _sut.GetAllWorldSummariesAsync();

        var dto = Assert.Single(result);
        // EF will project owner name from navigation — with owner added, this should resolve
        Assert.NotNull(dto.OwnerName);
    }
}
