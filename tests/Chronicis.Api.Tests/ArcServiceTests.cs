using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArcServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ArcService _service;

    public ArcServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new ArcService(_context, NullLogger<ArcService>.Instance);

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

        // Add a campaign
        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id,
            name: "Test Campaign");
        campaign.OwnerId = gm.Id;

        _context.Campaigns.Add(campaign);

        // Add two arcs with Creator loaded
        var arc1 = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id,
            name: "Act 1",
            sortOrder: 1);
        arc1.CreatedBy = gm.Id;
        arc1.Creator = gm;

        var arc2 = new Arc
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            CampaignId = campaign.Id,
            Name = "Act 2",
            SortOrder = 2,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = gm.Id,
            Creator = gm
        };

        _context.Arcs.AddRange(arc1, arc2);

        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  GetArcsByCampaignAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetArcsByCampaignAsync_Member_ReturnsArcs()
    {
        var arcs = await _service.GetArcsByCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.Equal(2, arcs.Count);
        Assert.Equal("Act 1", arcs[0].Name);
        Assert.Equal("Act 2", arcs[1].Name);
        Assert.Equal(1, arcs[0].SortOrder);
        Assert.Equal(2, arcs[1].SortOrder);
    }

    [Fact]
    public async Task GetArcsByCampaignAsync_NonMember_ReturnsEmpty()
    {
        var arcs = await _service.GetArcsByCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User3);

        Assert.Empty(arcs);
    }

    [Fact]
    public async Task GetArcsByCampaignAsync_OrderedBySortOrder()
    {
        var arcs = await _service.GetArcsByCampaignAsync(TestHelpers.FixedIds.Campaign1, TestHelpers.FixedIds.User1);

        Assert.Equal(1, arcs[0].SortOrder);
        Assert.Equal(2, arcs[1].SortOrder);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetArcAsync_Member_ReturnsArc()
    {
        var arc = await _service.GetArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.NotNull(arc);
        Assert.Equal("Act 1", arc!.Name);
        Assert.Equal(TestHelpers.FixedIds.Campaign1, arc.CampaignId);
    }

    [Fact]
    public async Task GetArcAsync_NonMember_ReturnsNull()
    {
        var arc = await _service.GetArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User3);

        Assert.Null(arc);
    }

    [Fact]
    public async Task GetArcAsync_NonExistent_ReturnsNull()
    {
        var arc = await _service.GetArcAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Null(arc);
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateArcAsync_Member_Succeeds()
    {
        var dto = new ArcCreateDto
        {
            CampaignId = TestHelpers.FixedIds.Campaign1,
            Name = "Act 3",
            Description = "The final act",
            SortOrder = 3
        };

        var arc = await _service.CreateArcAsync(dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(arc);
        Assert.Equal("Act 3", arc!.Name);
        Assert.Equal("The final act", arc.Description);
        Assert.Equal(3, arc.SortOrder);
    }

    [Fact]
    public async Task CreateArcAsync_AutoCalculatesSortOrder()
    {
        var dto = new ArcCreateDto
        {
            CampaignId = TestHelpers.FixedIds.Campaign1,
            Name = "Act 3",
            SortOrder = 0  // Auto-calculate
        };

        var arc = await _service.CreateArcAsync(dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(arc);
        Assert.Equal(3, arc!.SortOrder);  // Should be max(2) + 1
    }

    [Fact]
    public async Task CreateArcAsync_NonMember_ReturnsNull()
    {
        var dto = new ArcCreateDto
        {
            CampaignId = TestHelpers.FixedIds.Campaign1,
            Name = "Unauthorized Arc"
        };

        var arc = await _service.CreateArcAsync(dto, TestHelpers.FixedIds.User3);

        Assert.Null(arc);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArcAsync_Member_Succeeds()
    {
        var dto = new ArcUpdateDto
        {
            Name = "Updated Act 1",
            Description = "New description",
            SortOrder = 10
        };

        var arc = await _service.UpdateArcAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User1);

        Assert.NotNull(arc);
        Assert.Equal("Updated Act 1", arc!.Name);
        Assert.Equal("New description", arc.Description);
        Assert.Equal(10, arc.SortOrder);
    }

    [Fact]
    public async Task UpdateArcAsync_NonMember_ReturnsNull()
    {
        var dto = new ArcUpdateDto
        {
            Name = "Unauthorized Update"
        };

        var arc = await _service.UpdateArcAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User3);

        Assert.Null(arc);

        // Verify no changes
        var unchanged = await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1);
        Assert.Equal("Act 1", unchanged!.Name);
    }

    [Fact]
    public async Task UpdateArcAsync_NonExistent_ReturnsNull()
    {
        var dto = new ArcUpdateDto
        {
            Name = "Nothing to Update"
        };

        var arc = await _service.UpdateArcAsync(Guid.NewGuid(), dto, TestHelpers.FixedIds.User1);

        Assert.Null(arc);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteArcAsync_EmptyArc_Succeeds()
    {
        var success = await _service.DeleteArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.True(success);
        Assert.Null(await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1));
    }

    [Fact]
    public async Task DeleteArcAsync_WithSessions_Fails()
    {
        // Add a session to the arc
        _context.Articles.Add(TestHelpers.CreateArticle(
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User1,
            type: ArticleType.Session,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1));
        await _context.SaveChangesAsync();

        var success = await _service.DeleteArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.False(success);
        Assert.NotNull(await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1));
    }

    [Fact]
    public async Task DeleteArcAsync_NonMember_ReturnsFalse()
    {
        var success = await _service.DeleteArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User3);

        Assert.False(success);
        Assert.NotNull(await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1));
    }

    [Fact]
    public async Task DeleteArcAsync_NonExistent_ReturnsFalse()
    {
        var success = await _service.DeleteArcAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.False(success);
    }

    // ────────────────────────────────────────────────────────────────
    //  ActivateArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateArcAsync_Member_Succeeds()
    {
        var success = await _service.ActivateArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.True(success);

        var arc = await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1);
        Assert.True(arc!.IsActive);
    }

    [Fact]
    public async Task ActivateArcAsync_DeactivatesOthers()
    {
        var arc2Id = Guid.Parse("30000000-0000-0000-0000-000000000002");
        
        // Activate Arc 2 first
        var arc2 = await _context.Arcs.FindAsync(arc2Id);
        arc2!.IsActive = true;
        await _context.SaveChangesAsync();

        // Activate Arc 1
        var success = await _service.ActivateArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.True(success);

        // Verify Arc 1 is active, Arc 2 is not
        var arc1 = await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1);
        var arc2Reloaded = await _context.Arcs.FindAsync(arc2Id);

        Assert.True(arc1!.IsActive);
        Assert.False(arc2Reloaded!.IsActive);
    }

    [Fact]
    public async Task ActivateArcAsync_NonMember_ReturnsFalse()
    {
        var success = await _service.ActivateArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User3);

        Assert.False(success);

        var arc = await _context.Arcs.FindAsync(TestHelpers.FixedIds.Arc1);
        Assert.False(arc!.IsActive);
    }

    [Fact]
    public async Task ActivateArcAsync_NonExistent_ReturnsFalse()
    {
        var success = await _service.ActivateArcAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.False(success);
    }
}
