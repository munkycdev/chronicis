using System.Diagnostics.CodeAnalysis;
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

[ExcludeFromCodeCoverage]
public class QuestUpdateServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly QuestUpdateService _service;
    private readonly Guid _questId = Guid.Parse("60000000-0000-0000-0000-000000000001");

    public QuestUpdateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new QuestUpdateService(_context, NullLogger<QuestUpdateService>.Instance);

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

        // Add observer user
        var observer = TestHelpers.CreateUser(id: TestHelpers.FixedIds.User3);
        _context.Users.Add(observer);
        _context.WorldMembers.Add(TestHelpers.CreateWorldMember(
            worldId: world.Id,
            userId: observer.Id,
            role: WorldRole.Observer));

        // Add campaign and arc
        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id);
        _context.Campaigns.Add(campaign);

        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id);
        arc.Creator = gm;
        _context.Arcs.Add(arc);

        // Add a quest
        var quest = new Quest
        {
            Id = _questId,
            ArcId = arc.Id,
            Title = "Find the Artifact",
            Status = QuestStatus.Active,
            IsGmOnly = false,
            CreatedBy = gm.Id,
            Creator = gm,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Quests.Add(quest);

        // Add some quest updates
        var update1 = new QuestUpdate
        {
            Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
            QuestId = _questId,
            Body = "First update",
            CreatedBy = gm.Id,
            Creator = gm,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var update2 = new QuestUpdate
        {
            Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
            QuestId = _questId,
            Body = "Second update",
            CreatedBy = player.Id,
            Creator = player,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.QuestUpdates.AddRange(update1, update2);
        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  GetQuestUpdatesAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestUpdatesAsync_ValidRequest_ReturnsUpdates()
    {
        var result = await _service.GetQuestUpdatesAsync(_questId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_OrdersByCreatedAtDescending()
    {
        var result = await _service.GetQuestUpdatesAsync(_questId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        var items = result.Value!.Items;
        Assert.Equal("Second update", items[0].Body); // Most recent first
        Assert.Equal("First update", items[1].Body);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_WithPagination_ReturnsSubset()
    {
        var result = await _service.GetQuestUpdatesAsync(_questId, TestHelpers.FixedIds.User1, skip: 1, take: 1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal("First update", result.Value.Items[0].Body);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_InvalidPagination_ReturnsValidationError()
    {
        var result = await _service.GetQuestUpdatesAsync(_questId, TestHelpers.FixedIds.User1, skip: -1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("non-negative", result.ErrorMessage!);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_TakeTooLarge_ReturnsValidationError()
    {
        var result = await _service.GetQuestUpdatesAsync(_questId, TestHelpers.FixedIds.User1, take: 101);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("between 1 and 100", result.ErrorMessage!);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_NonExistentQuest_ReturnsNotFound()
    {
        var result = await _service.GetQuestUpdatesAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    // ────────────────────────────────────────────────────────────────
    //  CreateQuestUpdateAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateQuestUpdateAsync_AsGM_Succeeds()
    {
        var dto = new QuestUpdateCreateDto
        {
            Body = "New progress update"
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("New progress update", result.Value!.Body);
        Assert.Equal(_questId, result.Value.QuestId);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_AsPlayer_Succeeds()
    {
        var dto = new QuestUpdateCreateDto
        {
            Body = "Player progress"
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal("Player progress", result.Value!.Body);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_AsObserver_ReturnsForbidden()
    {
        var dto = new QuestUpdateCreateDto
        {
            Body = "Observer update"
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User3);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Contains("Observers", result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_EmptyBody_ReturnsValidationError()
    {
        var dto = new QuestUpdateCreateDto
        {
            Body = ""
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("required", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_UpdatesParentQuestTimestamp()
    {
        var originalUpdatedAt = (await _context.Quests.FindAsync(_questId))!.UpdatedAt;
        await Task.Delay(10);

        var dto = new QuestUpdateCreateDto
        {
            Body = "Timestamp test"
        };

        await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User1);

        var quest = await _context.Quests.FindAsync(_questId);
        Assert.True(quest!.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_WithSession_ValidatesSession()
    {
        // Create a session article
        var session = TestHelpers.CreateArticle(
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User1,
            type: ArticleType.Session,
            arcId: TestHelpers.FixedIds.Arc1);
        _context.Articles.Add(session);
        await _context.SaveChangesAsync();

        var dto = new QuestUpdateCreateDto
        {
            Body = "Session progress",
            SessionId = session.Id
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal(session.Id, result.Value!.SessionId);
    }

    [Fact]
    public async Task CreateQuestUpdateAsync_InvalidSessionType_ReturnsValidationError()
    {
        // Create a non-session article
        var article = TestHelpers.CreateArticle(
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User1,
            type: ArticleType.WikiArticle);
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        var dto = new QuestUpdateCreateDto
        {
            Body = "Progress",
            SessionId = article.Id
        };

        var result = await _service.CreateQuestUpdateAsync(_questId, dto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Contains("not a Session", result.ErrorMessage!);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteQuestUpdateAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteQuestUpdateAsync_AsGM_CanDeleteAny()
    {
        var updateId = Guid.Parse("70000000-0000-0000-0000-000000000002"); // Player's update

        var result = await _service.DeleteQuestUpdateAsync(_questId, updateId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.True(result.Value);

        var deleted = await _context.QuestUpdates.FindAsync(updateId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteQuestUpdateAsync_PlayerCanDeleteOwn()
    {
        var updateId = Guid.Parse("70000000-0000-0000-0000-000000000002"); // Player's update

        var result = await _service.DeleteQuestUpdateAsync(_questId, updateId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task DeleteQuestUpdateAsync_PlayerCannotDeleteOthers()
    {
        var updateId = Guid.Parse("70000000-0000-0000-0000-000000000001"); // GM's update

        var result = await _service.DeleteQuestUpdateAsync(_questId, updateId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Contains("your own", result.ErrorMessage!);

        var notDeleted = await _context.QuestUpdates.FindAsync(updateId);
        Assert.NotNull(notDeleted);
    }

    [Fact]
    public async Task DeleteQuestUpdateAsync_NonExistent_ReturnsNotFound()
    {
        var result = await _service.DeleteQuestUpdateAsync(_questId, Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }
}
