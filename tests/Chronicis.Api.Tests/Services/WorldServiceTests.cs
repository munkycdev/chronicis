using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;


[ExcludeFromCodeCoverage]
public class WorldServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly WorldService _service;
    private readonly IWorldMembershipService _membershipService;

    public WorldServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);

        _membershipService = Substitute.For<IWorldMembershipService>();

        _service = new WorldService(
            _context,
            _membershipService,
            Substitute.For<IReservedSlugProvider>(),
            NullLogger<WorldService>.Instance);

        SeedTestData();
    }

    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    private void SeedTestData()
    {
        // Seed basic world with owner and member
        var (world, owner, member) = TestHelpers.SeedBasicWorld(_context);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetUserWorldsAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserWorldsAsync_Member_ReturnsWorlds()
    {
        var worlds = await _service.GetUserWorldsAsync(TestHelpers.FixedIds.User1);

        Assert.Single(worlds);
        Assert.Equal("Test World", worlds[0].Name);
        Assert.Equal(TestHelpers.FixedIds.User1, worlds[0].OwnerId);
    }

    [Fact]
    public async Task GetUserWorldsAsync_MemberOfMultipleWorlds_ReturnsAll()
    {
        // Create second world
        var world2 = TestHelpers.CreateWorld(name: "World 2", ownerId: TestHelpers.FixedIds.User2);
        _context.Worlds.Add(world2);
        _context.WorldMembers.Add(TestHelpers.CreateWorldMember(
            worldId: world2.Id,
            userId: TestHelpers.FixedIds.User1,
            role: WorldRole.Player));
        await _context.SaveChangesAsync();

        var worlds = await _service.GetUserWorldsAsync(TestHelpers.FixedIds.User1);

        Assert.Equal(2, worlds.Count);
    }

    [Fact]
    public async Task GetUserWorldsAsync_NonMember_ReturnsEmpty()
    {
        var worlds = await _service.GetUserWorldsAsync(TestHelpers.FixedIds.User3);

        Assert.Empty(worlds);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetWorldAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorldAsync_Member_ReturnsWorld()
    {
        _membershipService.UserHasAccessAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1)
            .Returns(true);

        var world = await _service.GetWorldAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1);

        Assert.NotNull(world);
        Assert.Equal("Test World", world!.Name);
        Assert.Equal(TestHelpers.FixedIds.User1, world.OwnerId);
    }

    [Fact]
    public async Task GetWorldAsync_NonMember_ReturnsNull()
    {
        _membershipService.UserHasAccessAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User3)
            .Returns(false);

        var world = await _service.GetWorldAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User3);

        Assert.Null(world);
    }

    [Fact]
    public async Task GetWorldAsync_NonExistent_ReturnsNull()
    {
        var world = await _service.GetWorldAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Null(world);
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateWorldAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorldAsync_ValidInput_CreatesWorld()
    {
        var dto = new WorldCreateDto
        {
            Name = "New World",
            Description = "A brand new adventure"
        };

        var world = await _service.CreateWorldAsync(dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(world);
        Assert.Equal("New World", world.Name);
        Assert.Equal("A brand new adventure", world.Description);
        Assert.Equal(TestHelpers.FixedIds.User1, world.OwnerId);
        Assert.NotEmpty(world.Slug);
    }

    [Fact]
    public async Task CreateWorldAsync_GeneratesSlugFromName()
    {
        var dto = new WorldCreateDto
        {
            Name = "My Awesome World"
        };

        var world = await _service.CreateWorldAsync(dto, TestHelpers.FixedIds.User1);

        Assert.Equal("my-awesome-world", world.Slug);
    }

    [Fact]
    public async Task CreateWorldAsync_CreatesDefaultStructure()
    {
        var dto = new WorldCreateDto
        {
            Name = "Structured World"
        };

        var world = await _service.CreateWorldAsync(dto, TestHelpers.FixedIds.User1);

        // Verify default wiki articles were created
        var articles = await _context.Articles.Where(a => a.WorldId == world.Id).ToListAsync();
        Assert.Contains(articles, a => a.Title == "Bestiary");
        Assert.Contains(articles, a => a.Title == "Characters");
        Assert.Contains(articles, a => a.Title == "Factions");
        Assert.Contains(articles, a => a.Title == "Locations");
        Assert.Contains(articles, a => a.Title == "New Character" && a.Type == ArticleType.Character);

        // Verify default campaign and arc were created
        var campaigns = await _context.Campaigns.Where(c => c.WorldId == world.Id).ToListAsync();
        Assert.Single(campaigns);
        Assert.Equal("Campaign 1", campaigns[0].Name);

        var arcs = await _context.Arcs.Where(a => a.CampaignId == campaigns[0].Id).ToListAsync();
        Assert.Single(arcs);
        Assert.Equal("Arc 1", arcs[0].Name);
    }

    [Fact]
    public async Task CreateWorldAsync_AddsCreatorAsGmMember()
    {
        var dto = new WorldCreateDto { Name = "Membership Test World" };

        var world = await _service.CreateWorldAsync(dto, TestHelpers.FixedIds.User1);

        // The creator must be a WorldMember with GM role so GetUserWorldsAsync can return it
        var member = await _context.WorldMembers
            .FirstOrDefaultAsync(m => m.WorldId == world.Id && m.UserId == TestHelpers.FixedIds.User1);

        Assert.NotNull(member);
        Assert.Equal(WorldRole.GM, member.Role);
        Assert.Null(member.InvitedBy); // Self-created, no inviter
    }

    [Fact]
    public async Task CreateWorldAsync_DuplicateSlug_GeneratesUnique()
    {
        // Create first world
        var dto1 = new WorldCreateDto { Name = "Duplicate" };
        var world1 = await _service.CreateWorldAsync(dto1, TestHelpers.FixedIds.User1);

        // Create second world with same name
        var dto2 = new WorldCreateDto { Name = "Duplicate" };
        var world2 = await _service.CreateWorldAsync(dto2, TestHelpers.FixedIds.User1);

        Assert.NotEqual(world1.Slug, world2.Slug);
        Assert.Equal("duplicate", world1.Slug);
        Assert.Equal("duplicate-2", world2.Slug);
    }

    [Fact]
    public async Task CreateWorldAsync_NonExistentUser_ThrowsInvalidOperation()
    {
        var dto = new WorldCreateDto { Name = "Invalid User World" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateWorldAsync(dto, Guid.NewGuid()));
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateWorldAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWorldAsync_AsOwner_Succeeds()
    {
        var dto = new WorldUpdateDto
        {
            Name = "Updated World",
            Description = "Updated description"
        };

        var world = await _service.UpdateWorldAsync(TestHelpers.FixedIds.World1, dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(world);
        Assert.Equal("Updated World", world!.Name);
        Assert.Equal("Updated description", world.Description);
    }

    [Fact]
    public async Task UpdateWorldAsync_NonOwner_ReturnsNull()
    {
        var dto = new WorldUpdateDto
        {
            Name = "Hacked World"
        };

        var world = await _service.UpdateWorldAsync(TestHelpers.FixedIds.World1, dto, TestHelpers.FixedIds.User2);

        Assert.Null(world);

        // Verify no changes
        var unchanged = await _context.Worlds.FindAsync(TestHelpers.FixedIds.World1);
        Assert.Equal("Test World", unchanged!.Name);
    }

    [Fact]
    public async Task UpdateWorldAsync_NonExistent_ReturnsNull()
    {
        var dto = new WorldUpdateDto
        {
            Name = "Update Nothing"
        };

        var world = await _service.UpdateWorldAsync(Guid.NewGuid(), dto, TestHelpers.FixedIds.User1);

        Assert.Null(world);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetWorldBySlugAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorldBySlugAsync_ValidSlug_ReturnsWorld()
    {
        _membershipService.UserHasAccessAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1)
            .Returns(true);

        var world = await _service.GetWorldBySlugAsync("test-world", TestHelpers.FixedIds.User1);

        Assert.NotNull(world);
        Assert.Equal("Test World", world!.Name);
    }

    [Fact]
    public async Task GetWorldBySlugAsync_NonExistentSlug_ReturnsNull()
    {
        var world = await _service.GetWorldBySlugAsync("nonexistent", TestHelpers.FixedIds.User1);

        Assert.Null(world);
    }

    [Fact]
    public async Task GetWorldBySlugAsync_NonMember_ReturnsNull()
    {
        _membershipService.UserHasAccessAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User3)
            .Returns(false);

        var world = await _service.GetWorldBySlugAsync("test-world", TestHelpers.FixedIds.User3);

        Assert.Null(world);
    }

    [Fact]
    public void Mapping_UsesFallbacks_WhenOwnerCampaignsAndMembersMissing()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Fallback World",
            Slug = "fallback-world",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Owner = null!,
            Campaigns = null!,
            Members = null!
        };

        var dto = WorldService.MapToDto(world);
        var detail = WorldService.MapToDetailDto(world);

        Assert.Equal("Unknown", dto.OwnerName);
        Assert.Equal(0, dto.CampaignCount);
        Assert.Equal(0, dto.MemberCount);
        Assert.Equal("Unknown", detail.OwnerName);
        Assert.Equal(0, detail.CampaignCount);
        Assert.Equal(0, detail.MemberCount);
        Assert.Empty(detail.Campaigns);
        Assert.Empty(detail.Members);
    }

    [Fact]
    public void Mapping_PublicWorld_ExposesSlug()
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = "Public World",
            Slug = "public-world",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IsPublic = true,
            Owner = null!,
            Campaigns = null!,
            Members = null!
        };

        var dto = WorldService.MapToDto(world);
        var detail = WorldService.MapToDetailDto(world);

        Assert.Equal("public-world", dto.Slug);
        Assert.Equal("public-world", detail.Slug);
    }

    // ────────────────────────────────────────────────────────────────
    //  Reserved slug enforcement
    // ────────────────────────────────────────────────────────────────

    private WorldService CreateServiceWithReservedSlugs(params string[] reserved)
    {
        var options = Options.Create(new RoutingOptions
        {
            ReservedSlugs = new HashSet<string>(reserved, StringComparer.OrdinalIgnoreCase)
        });
        var provider = new ReservedSlugProvider(options);
        return new WorldService(
            _context,
            _membershipService,
            provider,
            NullLogger<WorldService>.Instance);
    }

    [Fact]
    public async Task CreateWorldAsync_NameGeneratesReservedSlug_AppendsSuffix()
    {
        // "Dashboard" generates slug "dashboard" which is reserved → should become "dashboard-2"
        var service = CreateServiceWithReservedSlugs("dashboard");
        var dto = new WorldCreateDto { Name = "Dashboard" };

        var world = await service.CreateWorldAsync(dto, TestHelpers.FixedIds.User1);

        Assert.Equal("dashboard-2", world.Slug);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetIdBySlugAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIdBySlugAsync_ExistingSlug_ReturnsWorldInfo()
    {
        var result = await _service.GetIdBySlugAsync("test-world");

        Assert.NotNull(result);
        Assert.Equal(TestHelpers.FixedIds.World1, result!.Value.Id);
        Assert.Equal("Test World", result.Value.Name);
    }

    [Fact]
    public async Task GetIdBySlugAsync_UnknownSlug_ReturnsNull()
    {
        var result = await _service.GetIdBySlugAsync("no-such-world");

        Assert.Null(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateSlugAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSlugAsync_ValidSlug_UpdatesAndReturnsNewSlug()
    {
        var result = await _service.UpdateSlugAsync(TestHelpers.FixedIds.World1, "new-world-slug", TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        var world = await _context.Worlds.FindAsync(TestHelpers.FixedIds.World1);
        Assert.Equal(result.Value, world!.Slug);
    }

    [Fact]
    public async Task UpdateSlugAsync_WorldNotFound_ReturnsNotFound()
    {
        var result = await _service.UpdateSlugAsync(Guid.NewGuid(), "new-slug", TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateSlugAsync_NotOwner_ReturnsForbidden()
    {
        var result = await _service.UpdateSlugAsync(TestHelpers.FixedIds.World1, "new-slug", TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task UpdateSlugAsync_InvalidSlug_ReturnsValidationError()
    {
        var result = await _service.UpdateSlugAsync(TestHelpers.FixedIds.World1, "Bad Slug!", TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Equal("SLUG_INVALID", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateSlugAsync_ReservedSlug_ReturnsValidationError()
    {
        var service = CreateServiceWithReservedSlugs("admin");

        var result = await service.UpdateSlugAsync(TestHelpers.FixedIds.World1, "admin", TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Equal("SLUG_RESERVED", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateSlugAsync_SlugCollision_GeneratesUniqueSlug()
    {
        // Create a second world occupying "unique-slug"
        var world2 = TestHelpers.CreateWorld(id: Guid.NewGuid(), ownerId: TestHelpers.FixedIds.User1, slug: "unique-slug");
        _context.Worlds.Add(world2);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateSlugAsync(TestHelpers.FixedIds.World1, "unique-slug", TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotEqual("unique-slug", result.Value); // Collision resolved
    }
}
