using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class WorldMembershipServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly WorldMembershipService _service;

    private static readonly Guid WorldId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid GmUserId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid PlayerId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid ObserverId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private static readonly Guid NonMemberId = Guid.Parse("40000000-0000-0000-0000-000000000099");

    private static readonly Guid OwnerMemberId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid GmMemberId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid PlayerMemberId = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly Guid ObserverMemberId = Guid.Parse("50000000-0000-0000-0000-000000000004");

    public WorldMembershipServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new WorldMembershipService(
            _context,
            NullLogger<WorldMembershipService>.Instance);

        SeedTestData();
    }

    private bool _disposed = false;
    public void Dispose()
    {

        Dispose(true);
        // Suppress finalization, as cleanup has been done.
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
        _context.Users.AddRange(
            new User { Id = OwnerId, Auth0UserId = "auth0|owner", Email = "owner@test.com", DisplayName = "Owner" },
            new User { Id = GmUserId, Auth0UserId = "auth0|gm", Email = "gm@test.com", DisplayName = "GM User" },
            new User { Id = PlayerId, Auth0UserId = "auth0|player", Email = "player@test.com", DisplayName = "Player" },
            new User { Id = ObserverId, Auth0UserId = "auth0|observer", Email = "observer@test.com", DisplayName = "Observer" },
            new User { Id = NonMemberId, Auth0UserId = "auth0|nonmember", Email = "nonmember@test.com", DisplayName = "Non-Member" }
        );

        _context.Worlds.Add(new World
        {
            Id = WorldId,
            Name = "Test World",
            Slug = "test-world",
            OwnerId = OwnerId,
            CreatedAt = DateTime.UtcNow
        });

        _context.WorldMembers.AddRange(
            new WorldMember { Id = OwnerMemberId, WorldId = WorldId, UserId = OwnerId, Role = WorldRole.GM, JoinedAt = DateTime.UtcNow },
            new WorldMember { Id = GmMemberId, WorldId = WorldId, UserId = GmUserId, Role = WorldRole.GM, JoinedAt = DateTime.UtcNow },
            new WorldMember { Id = PlayerMemberId, WorldId = WorldId, UserId = PlayerId, Role = WorldRole.Player, JoinedAt = DateTime.UtcNow },
            new WorldMember { Id = ObserverMemberId, WorldId = WorldId, UserId = ObserverId, Role = WorldRole.Observer, JoinedAt = DateTime.UtcNow }
        );

        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  Access checks
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserHasAccess_Member_ReturnsTrue()
    {
        Assert.True(await _service.UserHasAccessAsync(WorldId, OwnerId));
        Assert.True(await _service.UserHasAccessAsync(WorldId, PlayerId));
        Assert.True(await _service.UserHasAccessAsync(WorldId, ObserverId));
    }

    [Fact]
    public async Task UserHasAccess_NonMember_ReturnsFalse()
    {
        Assert.False(await _service.UserHasAccessAsync(WorldId, NonMemberId));
    }

    [Fact]
    public async Task UserHasAccess_NonExistentWorld_ReturnsFalse()
    {
        Assert.False(await _service.UserHasAccessAsync(Guid.NewGuid(), OwnerId));
    }

    [Fact]
    public async Task UserOwnsWorld_Owner_ReturnsTrue()
    {
        Assert.True(await _service.UserOwnsWorldAsync(WorldId, OwnerId));
    }

    [Fact]
    public async Task UserOwnsWorld_NonOwner_ReturnsFalse()
    {
        Assert.False(await _service.UserOwnsWorldAsync(WorldId, GmUserId));
        Assert.False(await _service.UserOwnsWorldAsync(WorldId, PlayerId));
    }

    // ────────────────────────────────────────────────────────────────
    //  GetMembers
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMembers_AsMember_ReturnsAllMembers()
    {
        var members = await _service.GetMembersAsync(WorldId, PlayerId);

        Assert.Equal(4, members.Count);
    }

    [Fact]
    public async Task GetMembers_AsNonMember_ReturnsEmpty()
    {
        var members = await _service.GetMembersAsync(WorldId, NonMemberId);

        Assert.Empty(members);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateMemberRole
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMemberRole_AsGM_Succeeds()
    {
        var dto = new WorldMemberUpdateDto { Role = WorldRole.Observer };

        var result = await _service.UpdateMemberRoleAsync(WorldId, PlayerMemberId, dto, OwnerId);

        Assert.NotNull(result);
        Assert.Equal(WorldRole.Observer, result!.Role);
    }

    [Fact]
    public async Task UpdateMemberRole_AsPlayer_ReturnNull()
    {
        var dto = new WorldMemberUpdateDto { Role = WorldRole.GM };

        var result = await _service.UpdateMemberRoleAsync(WorldId, ObserverMemberId, dto, PlayerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateMemberRole_LastGM_CannotDemote()
    {
        // Remove second GM so only owner remains as GM
        var secondGm = await _context.WorldMembers.FindAsync(GmMemberId);
        secondGm!.Role = WorldRole.Player;
        await _context.SaveChangesAsync();

        var dto = new WorldMemberUpdateDto { Role = WorldRole.Player };

        var result = await _service.UpdateMemberRoleAsync(WorldId, OwnerMemberId, dto, OwnerId);

        // Should fail — can't demote the last GM
        Assert.Null(result);

        // Verify role unchanged in DB
        var owner = await _context.WorldMembers.FindAsync(OwnerMemberId);
        Assert.Equal(WorldRole.GM, owner!.Role);
    }

    [Fact]
    public async Task UpdateMemberRole_NonExistentMember_ReturnsNull()
    {
        var dto = new WorldMemberUpdateDto { Role = WorldRole.Player };

        var result = await _service.UpdateMemberRoleAsync(WorldId, Guid.NewGuid(), dto, OwnerId);

        Assert.Null(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  RemoveMember
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMember_AsGM_Succeeds()
    {
        var result = await _service.RemoveMemberAsync(WorldId, PlayerMemberId, OwnerId);

        Assert.True(result);
        Assert.Null(await _context.WorldMembers.FindAsync(PlayerMemberId));
    }

    [Fact]
    public async Task RemoveMember_AsPlayer_Fails()
    {
        var result = await _service.RemoveMemberAsync(WorldId, ObserverMemberId, PlayerId);

        Assert.False(result);
        // Member should still exist
        Assert.NotNull(await _context.WorldMembers.FindAsync(ObserverMemberId));
    }

    [Fact]
    public async Task RemoveMember_LastGM_CannotRemove()
    {
        // Demote second GM first
        var secondGm = await _context.WorldMembers.FindAsync(GmMemberId);
        secondGm!.Role = WorldRole.Player;
        await _context.SaveChangesAsync();

        var result = await _service.RemoveMemberAsync(WorldId, OwnerMemberId, OwnerId);

        Assert.False(result);
        Assert.NotNull(await _context.WorldMembers.FindAsync(OwnerMemberId));
    }

    [Fact]
    public async Task RemoveMember_NonExistentMember_ReturnsFalse()
    {
        var result = await _service.RemoveMemberAsync(WorldId, Guid.NewGuid(), OwnerId);

        Assert.False(result);
    }
}
