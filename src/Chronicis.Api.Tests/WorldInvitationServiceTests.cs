using Xunit;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chronicis.Api.Tests;

public class WorldInvitationServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly WorldInvitationService _service;

    private static readonly Guid WorldId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid GmUserId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid PlayerId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid NewUserId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid GmMemberId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid PlayerMemberId = Guid.Parse("50000000-0000-0000-0000-000000000002");

    public WorldInvitationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new WorldInvitationService(
            _context,
            NullLogger<WorldInvitationService>.Instance);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        _context.Users.AddRange(
            new User { Id = GmUserId, Auth0UserId = "auth0|gm", Email = "gm@test.com", DisplayName = "GM" },
            new User { Id = PlayerId, Auth0UserId = "auth0|player", Email = "player@test.com", DisplayName = "Player" },
            new User { Id = NewUserId, Auth0UserId = "auth0|new", Email = "new@test.com", DisplayName = "New User" }
        );

        _context.Worlds.Add(new World
        {
            Id = WorldId,
            Name = "Test World",
            Slug = "test-world",
            OwnerId = GmUserId,
            CreatedAt = DateTime.UtcNow
        });

        _context.WorldMembers.AddRange(
            new WorldMember { Id = GmMemberId, WorldId = WorldId, UserId = GmUserId, Role = WorldRole.GM, JoinedAt = DateTime.UtcNow },
            new WorldMember { Id = PlayerMemberId, WorldId = WorldId, UserId = PlayerId, Role = WorldRole.Player, JoinedAt = DateTime.UtcNow }
        );

        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateInvitation
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvitation_AsGM_Succeeds()
    {
        var dto = new WorldInvitationCreateDto { Role = WorldRole.Player };

        var result = await _service.CreateInvitationAsync(WorldId, dto, GmUserId);

        Assert.NotNull(result);
        Assert.Equal(WorldId, result!.WorldId);
        Assert.Equal(WorldRole.Player, result.Role);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.UsedCount);
        Assert.Matches(@"^[A-Z]{4}-[A-Z]{4}$", result.Code);
    }

    [Fact]
    public async Task CreateInvitation_AsPlayer_ReturnsNull()
    {
        var dto = new WorldInvitationCreateDto { Role = WorldRole.Player };

        var result = await _service.CreateInvitationAsync(WorldId, dto, PlayerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateInvitation_WithExpiration_SetsExpiresAt()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var dto = new WorldInvitationCreateDto { Role = WorldRole.Player, ExpiresAt = expiresAt };

        var result = await _service.CreateInvitationAsync(WorldId, dto, GmUserId);

        Assert.NotNull(result);
        Assert.NotNull(result!.ExpiresAt);
    }

    [Fact]
    public async Task CreateInvitation_WithMaxUses_SetsMaxUses()
    {
        var dto = new WorldInvitationCreateDto { Role = WorldRole.Player, MaxUses = 5 };

        var result = await _service.CreateInvitationAsync(WorldId, dto, GmUserId);

        Assert.NotNull(result);
        Assert.Equal(5, result!.MaxUses);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetInvitations
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvitations_AsGM_ReturnsAll()
    {
        // Seed two invitations
        await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);
        await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Observer }, GmUserId);

        var invitations = await _service.GetInvitationsAsync(WorldId, GmUserId);

        Assert.Equal(2, invitations.Count);
    }

    [Fact]
    public async Task GetInvitations_AsPlayer_ReturnsEmpty()
    {
        await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        var invitations = await _service.GetInvitationsAsync(WorldId, PlayerId);

        Assert.Empty(invitations);
    }

    // ────────────────────────────────────────────────────────────────
    //  RevokeInvitation
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeInvitation_AsGM_Succeeds()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        var result = await _service.RevokeInvitationAsync(WorldId, invitation!.Id, GmUserId);

        Assert.True(result);

        // Verify it's inactive in DB
        var dbInvitation = await _context.WorldInvitations.FindAsync(invitation.Id);
        Assert.False(dbInvitation!.IsActive);
    }

    [Fact]
    public async Task RevokeInvitation_AsPlayer_Fails()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        var result = await _service.RevokeInvitationAsync(WorldId, invitation!.Id, PlayerId);

        Assert.False(result);
    }

    // ────────────────────────────────────────────────────────────────
    //  JoinWorld
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinWorld_ValidCode_Succeeds()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        var result = await _service.JoinWorldAsync(invitation!.Code, NewUserId);

        Assert.True(result.Success);
        Assert.Equal(WorldId, result.WorldId);
        Assert.Equal(WorldRole.Player, result.AssignedRole);

        // Verify membership created
        var member = await _context.WorldMembers.FirstOrDefaultAsync(m => m.UserId == NewUserId && m.WorldId == WorldId);
        Assert.NotNull(member);
        Assert.Equal(WorldRole.Player, member!.Role);
    }

    [Fact]
    public async Task JoinWorld_InvalidCodeFormat_Fails()
    {
        var result = await _service.JoinWorldAsync("bad", NewUserId);

        Assert.False(result.Success);
        Assert.Contains("format", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_NonExistentCode_Fails()
    {
        var result = await _service.JoinWorldAsync("ABCD-EFGH", NewUserId);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_ExpiredInvitation_Fails()
    {
        // Create invitation then manually expire it
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);
        var dbInvitation = await _context.WorldInvitations.FindAsync(invitation!.Id);
        dbInvitation!.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _context.SaveChangesAsync();

        var result = await _service.JoinWorldAsync(invitation.Code, NewUserId);

        Assert.False(result.Success);
        Assert.Contains("expired", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_MaxUsesReached_Fails()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId,
            new WorldInvitationCreateDto { Role = WorldRole.Player, MaxUses = 1 }, GmUserId);

        // Use it once
        await _service.JoinWorldAsync(invitation!.Code, NewUserId);

        // Try to use it again with a different user
        var anotherUser = new User { Id = Guid.NewGuid(), Auth0UserId = "auth0|another", Email = "another@test.com", DisplayName = "Another" };
        _context.Users.Add(anotherUser);
        await _context.SaveChangesAsync();

        var result = await _service.JoinWorldAsync(invitation.Code, anotherUser.Id);

        Assert.False(result.Success);
        Assert.Contains("maximum", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_AlreadyMember_Fails()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        // Player is already a member
        var result = await _service.JoinWorldAsync(invitation!.Code, PlayerId);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_RevokedInvitation_Fails()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);
        await _service.RevokeInvitationAsync(WorldId, invitation!.Id, GmUserId);

        var result = await _service.JoinWorldAsync(invitation.Code, NewUserId);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JoinWorld_IncrementsUsedCount()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        await _service.JoinWorldAsync(invitation!.Code, NewUserId);

        var dbInvitation = await _context.WorldInvitations.FindAsync(invitation.Id);
        Assert.Equal(1, dbInvitation!.UsedCount);
    }

    [Fact]
    public async Task JoinWorld_AssignsCorrectRole()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId,
            new WorldInvitationCreateDto { Role = WorldRole.Observer }, GmUserId);

        var result = await _service.JoinWorldAsync(invitation!.Code, NewUserId);

        Assert.True(result.Success);
        Assert.Equal(WorldRole.Observer, result.AssignedRole);

        var member = await _context.WorldMembers.FirstOrDefaultAsync(m => m.UserId == NewUserId);
        Assert.Equal(WorldRole.Observer, member!.Role);
    }

    [Fact]
    public async Task JoinWorld_NormalizesCodeFormat()
    {
        var invitation = await _service.CreateInvitationAsync(WorldId, new WorldInvitationCreateDto { Role = WorldRole.Player }, GmUserId);

        // Use lowercase without hyphen
        var rawCode = invitation!.Code.Replace("-", "").ToLowerInvariant();
        var result = await _service.JoinWorldAsync(rawCode, NewUserId);

        Assert.True(result.Success);
    }
}
