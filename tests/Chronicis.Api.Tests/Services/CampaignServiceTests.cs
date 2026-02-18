using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
public class CampaignServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly CampaignService _service;

    public CampaignServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new CampaignService(_context, NullLogger<CampaignService>.Instance);

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
        // Seed basic world with GM and Player
        var (world, gm, player) = TestHelpers.SeedBasicWorld(_context);

        // Add a campaign owned by GM
        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id,
            name: "Test Campaign");
        campaign.OwnerId = gm.Id;
        campaign.Owner = gm;

        _context.Campaigns.Add(campaign);

        // Add an arc to the campaign
        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id,
            name: "Act 1");

        _context.Arcs.Add(arc);
        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  GetCampaignAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCampaignAsync_Member_ReturnsCampaign()
    {
        var campaign = await _service.GetCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.NotNull(campaign);
        Assert.Equal("Test Campaign", campaign!.Name);
        Assert.Single(campaign.Arcs);
    }

    [Fact]
    public async Task GetCampaignAsync_NonMember_ReturnsNull()
    {
        var campaign = await _service.GetCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User3);

        Assert.Null(campaign);
    }

    [Fact]
    public async Task GetCampaignAsync_NonExistent_ReturnsNull()
    {
        var campaign = await _service.GetCampaignAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Null(campaign);
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateCampaignAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCampaignAsync_AsGM_Succeeds()
    {
        var dto = new CampaignCreateDto
        {
            WorldId = TestHelpers.FixedIds.World1,
            Name = "New Campaign",
            Description = "A new adventure"
        };

        var campaign = await _service.CreateCampaignAsync(dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(campaign);
        Assert.Equal("New Campaign", campaign.Name);
        Assert.Equal("A new adventure", campaign.Description);
        Assert.Equal(TestHelpers.FixedIds.User1, campaign.OwnerId);
    }

    [Fact]
    public async Task CreateCampaignAsync_CreatesDefaultArc()
    {
        var dto = new CampaignCreateDto
        {
            WorldId = TestHelpers.FixedIds.World1,
            Name = "Campaign with Arc"
        };

        var campaign = await _service.CreateCampaignAsync(dto, TestHelpers.FixedIds.User1);

        // Verify arc was created
        var arc = await _context.Arcs.FirstOrDefaultAsync(a => a.CampaignId == campaign.Id);
        Assert.NotNull(arc);
        Assert.Equal("Act 1", arc!.Name);
        Assert.Equal(1, arc.SortOrder);
    }

    [Fact]
    public async Task CreateCampaignAsync_AsPlayer_ThrowsUnauthorized()
    {
        var dto = new CampaignCreateDto
        {
            WorldId = TestHelpers.FixedIds.World1,
            Name = "Unauthorized Campaign"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateCampaignAsync(dto, TestHelpers.FixedIds.User2));
    }

    [Fact]
    public async Task CreateCampaignAsync_NonExistentWorld_ThrowsInvalidOperation()
    {
        var dto = new CampaignCreateDto
        {
            WorldId = Guid.NewGuid(),
            Name = "Campaign in Nowhere"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCampaignAsync(dto, TestHelpers.FixedIds.User1));
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateCampaignAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCampaignAsync_AsGM_Succeeds()
    {
        var dto = new CampaignUpdateDto
        {
            Name = "Updated Campaign",
            Description = "Updated description",
            StartedAt = new DateTime(2025, 1, 1),
            EndedAt = new DateTime(2025, 12, 31)
        };

        var campaign = await _service.UpdateCampaignAsync(TestHelpers.FixedIds.Campaign1, dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(campaign);
        Assert.Equal("Updated Campaign", campaign!.Name);
        Assert.Equal("Updated description", campaign.Description);
        Assert.Equal(new DateTime(2025, 1, 1), campaign.StartedAt);
        Assert.Equal(new DateTime(2025, 12, 31), campaign.EndedAt);
    }

    [Fact]
    public async Task UpdateCampaignAsync_AsPlayer_ReturnsNull()
    {
        var dto = new CampaignUpdateDto
        {
            Name = "Unauthorized Update"
        };

        var campaign = await _service.UpdateCampaignAsync(TestHelpers.FixedIds.Campaign1, dto, TestHelpers.FixedIds.User2);

        Assert.Null(campaign);

        // Verify no changes were made
        var unchanged = await _context.Campaigns.FindAsync(TestHelpers.FixedIds.Campaign1);
        Assert.Equal("Test Campaign", unchanged!.Name);
    }

    [Fact]
    public async Task UpdateCampaignAsync_NonExistent_ReturnsNull()
    {
        var dto = new CampaignUpdateDto
        {
            Name = "Update Nothing"
        };

        var campaign = await _service.UpdateCampaignAsync(Guid.NewGuid(), dto, TestHelpers.FixedIds.User1);

        Assert.Null(campaign);
    }

    // ────────────────────────────────────────────────────────────────
    //  Access and role checks
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserHasAccessAsync_Member_ReturnsTrue()
    {
        var hasAccess = await _service.UserHasAccessAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task UserHasAccessAsync_NonMember_ReturnsFalse()
    {
        var hasAccess = await _service.UserHasAccessAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User3);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task UserIsGMAsync_GM_ReturnsTrue()
    {
        var isGM = await _service.UserIsGMAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.True(isGM);
    }

    [Fact]
    public async Task UserIsGMAsync_Player_ReturnsFalse()
    {
        var isGM = await _service.UserIsGMAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User2);

        Assert.False(isGM);
    }

    [Fact]
    public async Task GetUserRoleAsync_GM_ReturnsGMRole()
    {
        var role = await _service.GetUserRoleAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.Equal(WorldRole.GM, role);
    }

    [Fact]
    public async Task GetUserRoleAsync_Player_ReturnsPlayerRole()
    {
        var role = await _service.GetUserRoleAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User2);

        Assert.Equal(WorldRole.Player, role);
    }

    [Fact]
    public async Task GetUserRoleAsync_NonMember_ReturnsNull()
    {
        var role = await _service.GetUserRoleAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User3);

        Assert.Null(role);
    }

    // ────────────────────────────────────────────────────────────────
    //  ActivateCampaignAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateCampaignAsync_AsGM_Succeeds()
    {
        var success = await _service.ActivateCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.True(success);

        var campaign = await _context.Campaigns.FindAsync(TestHelpers.FixedIds.Campaign1);
        Assert.True(campaign!.IsActive);
    }

    [Fact]
    public async Task ActivateCampaignAsync_DeactivatesOthers()
    {
        // Create a second active campaign
        var campaign2 = TestHelpers.CreateCampaign(worldId: TestHelpers.FixedIds.World1, name: "Campaign 2");
        campaign2.IsActive = true;
        _context.Campaigns.Add(campaign2);
        await _context.SaveChangesAsync();

        // Activate first campaign
        var success = await _service.ActivateCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.True(success);

        // Verify first is active, second is not
        var campaign1 = await _context.Campaigns.FindAsync(TestHelpers.FixedIds.Campaign1);
        var campaign2Reloaded = await _context.Campaigns.FindAsync(campaign2.Id);

        Assert.True(campaign1!.IsActive);
        Assert.False(campaign2Reloaded!.IsActive);
    }

    [Fact]
    public async Task ActivateCampaignAsync_AsPlayer_ReturnsFalse()
    {
        var success = await _service.ActivateCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User2);

        Assert.False(success);

        var campaign = await _context.Campaigns.FindAsync(TestHelpers.FixedIds.Campaign1);
        Assert.False(campaign!.IsActive);
    }

    [Fact]
    public async Task ActivateCampaignAsync_NonExistent_ReturnsFalse()
    {
        var success = await _service.ActivateCampaignAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.False(success);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetActiveContextAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveContextAsync_WithActiveCampaign_ReturnsContext()
    {
        // Activate the campaign
        await _service.ActivateCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        var context = await _service.GetActiveContextAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1);

        Assert.Equal(TestHelpers.FixedIds.World1, context.WorldId);
        Assert.Equal(TestHelpers.FixedIds.Campaign1, context.CampaignId);
        Assert.Equal("Test Campaign", context.CampaignName);
    }

    [Fact]
    public async Task GetActiveContextAsync_WithActiveArc_ReturnsFullContext()
    {
        // Activate campaign and arc
        await _service.ActivateCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        var arc = await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1);
        arc!.IsActive = true;
        await _context.SaveChangesAsync();

        var context = await _service.GetActiveContextAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1);

        Assert.Equal(TestHelpers.FixedIds.Campaign1, context.CampaignId);
        Assert.Equal(TestHelpers.FixedIds.Arc1, context.ArcId);
        Assert.Equal("Act 1", context.ArcName);
    }

    [Fact]
    public async Task GetActiveContextAsync_NoActiveCampaign_ReturnsEmptyContext()
    {
        var context = await _service.GetActiveContextAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User1);

        Assert.Equal(TestHelpers.FixedIds.World1, context.WorldId);
        Assert.Null(context.CampaignId);
        Assert.Null(context.CampaignName);
    }

    [Fact]
    public async Task GetActiveContextAsync_NonMember_ReturnsEmptyContext()
    {
        var context = await _service.GetActiveContextAsync(TestHelpers.FixedIds.World1, TestHelpers.FixedIds.User3);

        Assert.Equal(TestHelpers.FixedIds.World1, context.WorldId);
        Assert.Null(context.CampaignId);
    }

    [Fact]
    public void Mapping_UsesFallbacks_WhenOwnerAndArcsMissing()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Name = "No Owner",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Owner = null!,
            Arcs = null!
        };

        var mapToDto = typeof(CampaignService).GetMethod("MapToDto", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapToDetailDto = typeof(CampaignService).GetMethod("MapToDetailDto", BindingFlags.NonPublic | BindingFlags.Static)!;

        var dto = (CampaignDto)mapToDto.Invoke(null, [campaign])!;
        var detail = (CampaignDetailDto)mapToDetailDto.Invoke(null, [campaign])!;

        Assert.Equal("Unknown", dto.OwnerName);
        Assert.Equal(0, dto.ArcCount);
        Assert.Equal("Unknown", detail.OwnerName);
        Assert.Equal(0, detail.ArcCount);
        Assert.Empty(detail.Arcs);
    }
}
