using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class QuestServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly QuestService _service;

    public QuestServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new QuestService(_context, NullLogger<QuestService>.Instance);

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

        // Add campaign and arc
        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id,
            name: "Test Campaign");
        _context.Campaigns.Add(campaign);

        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id,
            name: "Act 1");
        arc.Creator = gm;
        _context.Arcs.Add(arc);

        // Add a public quest and a GM-only quest
        var publicQuest = new Quest
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            ArcId = arc.Id,
            Title = "Find the MacGuffin",
            Description = "A public quest",
            Status = QuestStatus.Active,
            IsGmOnly = false,
            SortOrder = 1,
            CreatedBy = gm.Id,
            Creator = gm,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        var gmQuest = new Quest
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000002"),
            ArcId = arc.Id,
            Title = "Secret GM Quest",
            Description = "A GM-only quest",
            Status = QuestStatus.Active,
            IsGmOnly = true,
            SortOrder = 2,
            CreatedBy = gm.Id,
            Creator = gm,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 }
        };

        _context.Quests.AddRange(publicQuest, gmQuest);
        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  GetQuestsByArcAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestsByArcAsync_AsGM_ReturnsAllQuests()
    {
        var result = await _service.GetQuestsByArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, q => q.Title == "Find the MacGuffin");
        Assert.Contains(result.Value, q => q.Title == "Secret GM Quest");
    }

    [Fact]
    public async Task GetQuestsByArcAsync_AsPlayer_FiltersGMOnlyQuests()
    {
        var result = await _service.GetQuestsByArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!);
        Assert.Equal("Find the MacGuffin", result.Value[0].Title);
        Assert.DoesNotContain(result.Value, q => q.IsGmOnly);
    }

    [Fact]
    public async Task GetQuestsByArcAsync_NonMember_ReturnsNotFound()
    {
        var result = await _service.GetQuestsByArcAsync(TestHelpers.FixedIds.Arc1, TestHelpers.FixedIds.User3);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetQuestsByArcAsync_NonExistentArc_ReturnsNotFound()
    {
        var result = await _service.GetQuestsByArcAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetQuestAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestAsync_AsGM_ReturnsQuest()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var result = await _service.GetQuestAsync(questId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("Find the MacGuffin", result.Value!.Title);
    }

    [Fact]
    public async Task GetQuestAsync_PlayerAccessingPublicQuest_Succeeds()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var result = await _service.GetQuestAsync(questId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal("Find the MacGuffin", result.Value!.Title);
    }

    [Fact]
    public async Task GetQuestAsync_PlayerAccessingGMQuest_ReturnsNotFound()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        var result = await _service.GetQuestAsync(questId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetQuestAsync_NonMember_ReturnsNotFound()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var result = await _service.GetQuestAsync(questId, TestHelpers.FixedIds.User3);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateQuestAsync
    // ────────────────────────────────────────────────────────────────

    [Fact(Skip = "EF InMemory requires explicit RowVersion - works in real SQL Server")]
    public async Task CreateQuestAsync_AsGM_Succeeds()
    {
        var dto = new QuestCreateDto
        {
            Title = "New Quest",
            Description = "A new adventure",
            Status = QuestStatus.Active,
            IsGmOnly = false,
            SortOrder = 3
        };

        var result = await _service.CreateQuestAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("New Quest", result.Value!.Title);
        Assert.Equal("A new adventure", result.Value.Description);
        Assert.Equal(QuestStatus.Active, result.Value.Status);
        Assert.Equal(3, result.Value.SortOrder);
        
        // Verify it was saved to database
        var saved = await _context.Quests.FindAsync(result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Quest", saved!.Title);
    }

    [Fact]
    public async Task CreateQuestAsync_AsPlayer_ReturnsForbidden()
    {
        var dto = new QuestCreateDto
        {
            Title = "Unauthorized Quest"
        };

        var result = await _service.CreateQuestAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Null(result.Value);
        Assert.Contains("Only GMs", result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateQuestAsync_EmptyTitle_ReturnsValidationError()
    {
        var dto = new QuestCreateDto
        {
            Title = ""
        };

        var result = await _service.CreateQuestAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("required", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuestAsync_TitleTooLong_ReturnsValidationError()
    {
        var dto = new QuestCreateDto
        {
            Title = new string('x', 301)
        };

        var result = await _service.CreateQuestAsync(TestHelpers.FixedIds.Arc1, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("300 characters", result.ErrorMessage!);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateQuestAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateQuestAsync_AsGM_Succeeds()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var quest = await _context.Quests.FindAsync(questId);
        var rowVersion = Convert.ToBase64String(quest!.RowVersion);

        var dto = new QuestEditDto
        {
            Title = "Updated Quest Title",
            Description = "Updated description",
            Status = QuestStatus.Completed,
            RowVersion = rowVersion
        };

        var result = await _service.UpdateQuestAsync(questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal("Updated Quest Title", result.Value!.Title);
        Assert.Equal("Updated description", result.Value.Description);
        Assert.Equal(QuestStatus.Completed, result.Value.Status);
    }

    [Fact]
    public async Task UpdateQuestAsync_AsPlayer_ReturnsForbidden()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var quest = await _context.Quests.FindAsync(questId);
        var rowVersion = Convert.ToBase64String(quest!.RowVersion);

        var dto = new QuestEditDto
        {
            Title = "Hacked Title",
            RowVersion = rowVersion
        };

        var result = await _service.UpdateQuestAsync(questId, dto, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Contains("Only GMs", result.ErrorMessage!);
    }

    [Fact(Skip = "EF InMemory does not support RowVersion concurrency checking")]
    public async Task UpdateQuestAsync_WithStaleRowVersion_ReturnsConflict()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var quest = await _context.Quests.FindAsync(questId);
        var oldRowVersion = Convert.ToBase64String(quest!.RowVersion);

        // Update quest to change RowVersion
        quest.Title = "Changed by someone else";
        await _context.SaveChangesAsync();

        // Try to update with stale RowVersion
        var dto = new QuestEditDto
        {
            Title = "My Update",
            RowVersion = oldRowVersion  // Stale!
        };

        var result = await _service.UpdateQuestAsync(questId, dto, TestHelpers.FixedIds.User1);

        // NOTE: EF InMemory doesn't actually enforce RowVersion concurrency
        // This test would pass in real SQL Server but not in InMemory
        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Contains("modified by another user", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        // Should return current state
        Assert.NotNull(result.Value);
        Assert.Equal("Changed by someone else", result.Value!.Title);
    }

    [Fact]
    public async Task UpdateQuestAsync_MissingRowVersion_ReturnsValidationError()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");

        var dto = new QuestEditDto
        {
            Title = "Updated",
            RowVersion = ""  // Missing!
        };

        var result = await _service.UpdateQuestAsync(questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("RowVersion is required", result.ErrorMessage!);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteQuestAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteQuestAsync_AsGM_Succeeds()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");

        var result = await _service.DeleteQuestAsync(questId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.True(result.Value);

        // Verify deleted
        var deleted = await _context.Quests.FindAsync(questId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteQuestAsync_CascadesToUpdates()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");

        // Add an update to the quest
        var update = new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = questId,
            Body = "Some progress",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };
        _context.QuestUpdates.Add(update);
        await _context.SaveChangesAsync();

        // Delete quest
        var result = await _service.DeleteQuestAsync(questId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);

        // Verify update was also deleted (cascade)
        var deletedUpdate = await _context.QuestUpdates.FindAsync(update.Id);
        Assert.Null(deletedUpdate);
    }

    [Fact]
    public async Task DeleteQuestAsync_AsPlayer_ReturnsForbidden()
    {
        var questId = Guid.Parse("60000000-0000-0000-0000-000000000001");

        var result = await _service.DeleteQuestAsync(questId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Contains("Only GMs", result.ErrorMessage!);

        // Verify not deleted
        var notDeleted = await _context.Quests.FindAsync(questId);
        Assert.NotNull(notDeleted);
    }

    [Fact]
    public async Task DeleteQuestAsync_NonExistent_ReturnsNotFound()
    {
        var result = await _service.DeleteQuestAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }
}
