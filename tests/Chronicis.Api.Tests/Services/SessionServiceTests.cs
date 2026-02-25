using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class SessionServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ISummaryService _summaryService;
    private readonly IWorldDocumentService _worldDocumentService;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _summaryService = Substitute.For<ISummaryService>();
        _worldDocumentService = Substitute.For<IWorldDocumentService>();
        _worldDocumentService.DeleteArticleImagesAsync(Arg.Any<Guid>()).Returns(Task.CompletedTask);
        _service = new SessionService(
            _context,
            _summaryService,
            _worldDocumentService,
            NullLogger<SessionService>.Instance);

        SeedTestData();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_NonGm_ReturnsForbidden()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = TestHelpers.FixedIds.Arc1,
            Name = "Session 1",
            PublicNotes = "Before public",
            PrivateNotes = "Before private",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var dto = new SessionUpdateDto
        {
            PublicNotes = "After public",
            PrivateNotes = "After private"
        };

        var result = await _service.UpdateSessionNotesAsync(session.Id, dto, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);

        var unchanged = await _context.Sessions.FindAsync(session.Id);
        Assert.NotNull(unchanged);
        Assert.Equal("Before public", unchanged!.PublicNotes);
        Assert.Equal("Before private", unchanged.PrivateNotes);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_Gm_CanUpdateSessionNameAndDate()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = TestHelpers.FixedIds.Arc1,
            Name = "Original Session",
            SessionDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            PublicNotes = "Public",
            PrivateNotes = "Private",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var updatedDate = new DateTime(2026, 2, 24, 0, 0, 0, DateTimeKind.Utc);
        var updateDto = new SessionUpdateDto
        {
            Name = "  Session Renamed  ",
            SessionDate = updatedDate,
            PublicNotes = "Public Updated",
            PrivateNotes = "Private Updated"
        };

        var updateResult = await _service.UpdateSessionNotesAsync(session.Id, updateDto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, updateResult.Status);
        Assert.NotNull(updateResult.Value);
        Assert.Equal("Session Renamed", updateResult.Value!.Name);
        Assert.Equal(updatedDate, updateResult.Value.SessionDate);

        var clearDateDto = new SessionUpdateDto
        {
            Name = "Session Renamed",
            ClearSessionDate = true,
            PublicNotes = "Public Updated",
            PrivateNotes = "Private Updated"
        };

        var clearDateResult = await _service.UpdateSessionNotesAsync(session.Id, clearDateDto, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, clearDateResult.Status);
        Assert.NotNull(clearDateResult.Value);
        Assert.Null(clearDateResult.Value!.SessionDate);

        var persisted = await _context.Sessions.FindAsync(session.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Session Renamed", persisted!.Name);
        Assert.Null(persisted.SessionDate);
        Assert.Equal("Public Updated", persisted.PublicNotes);
        Assert.Equal("Private Updated", persisted.PrivateNotes);
        Assert.NotNull(persisted.ModifiedAt);
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesExactlyOneDefaultPublicSessionNote()
    {
        var dto = new SessionCreateDto
        {
            Name = "The Dark Forest",
            SessionDate = new DateTime(2026, 2, 24, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.CreateSessionAsync(
            TestHelpers.FixedIds.Arc1,
            dto,
            TestHelpers.FixedIds.User1,
            "GM User");

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);

        var savedSession = await _context.Sessions.FindAsync(result.Value!.Id);
        Assert.NotNull(savedSession);

        var notes = await _context.Articles
            .Where(a => a.SessionId == result.Value.Id)
            .ToListAsync();

        Assert.Single(notes);

        var note = notes[0];
        Assert.Equal(ArticleType.SessionNote, note.Type);
        Assert.Equal(ArticleVisibility.Public, note.Visibility);
        Assert.Equal(result.Value.Id, note.SessionId);
        Assert.Equal("GM User's Notes", note.Title);
    }

    [Fact]
    public async Task GenerateAiSummaryAsync_UsesOnlyPublicSessionSources()
    {
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            ArcId = TestHelpers.FixedIds.Arc1,
            Name = "Session 7",
            PublicNotes = "PUBLIC_SESSION_NOTES",
            PrivateNotes = "PRIVATE_SESSION_NOTES",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);

        var publicNote = TestHelpers.CreateArticle(
            id: Guid.NewGuid(),
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User2,
            title: "Public Player Notes",
            slug: "public-player-notes",
            body: "PUBLIC_NOTE_BODY",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);
        publicNote.SessionId = sessionId;

        var membersOnlyNote = TestHelpers.CreateArticle(
            id: Guid.NewGuid(),
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User2,
            title: "Members Only Notes",
            slug: "members-only-notes",
            body: "MEMBERS_ONLY_NOTE_BODY",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.MembersOnly,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);
        membersOnlyNote.SessionId = sessionId;

        var privateNote = TestHelpers.CreateArticle(
            id: Guid.NewGuid(),
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User2,
            title: "Private Notes",
            slug: "private-notes",
            body: "PRIVATE_NOTE_BODY",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Private,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);
        privateNote.SessionId = sessionId;

        _context.Articles.AddRange(publicNote, membersOnlyNote, privateNote);
        await _context.SaveChangesAsync();

        string? capturedSourceContent = null;
        IReadOnlyList<SummarySourceDto>? capturedSources = null;

        _summaryService
            .GenerateSessionSummaryFromSourcesAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<SummarySourceDto>>(),
                Arg.Any<int>())
            .Returns(callInfo =>
            {
                capturedSourceContent = callInfo.ArgAt<string>(1);
                capturedSources = callInfo.ArgAt<IReadOnlyList<SummarySourceDto>>(2);

                return Task.FromResult(new SummaryGenerationDto
                {
                    Success = true,
                    Summary = "Generated summary"
                });
            });

        var result = await _service.GenerateAiSummaryAsync(sessionId, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.NotNull(capturedSourceContent);
        Assert.NotNull(capturedSources);

        Assert.Contains("PUBLIC_SESSION_NOTES", capturedSourceContent);
        Assert.Contains("PUBLIC_NOTE_BODY", capturedSourceContent);
        Assert.DoesNotContain("PRIVATE_SESSION_NOTES", capturedSourceContent);
        Assert.DoesNotContain("MEMBERS_ONLY_NOTE_BODY", capturedSourceContent);
        Assert.DoesNotContain("PRIVATE_NOTE_BODY", capturedSourceContent);

        Assert.Equal(2, capturedSources!.Count);
        Assert.Contains(capturedSources, s => s.Type == "SessionPublicNotes");
        Assert.Contains(capturedSources, s => s.Type == "SessionNote" && s.ArticleId == publicNote.Id);
        Assert.DoesNotContain(capturedSources, s => s.ArticleId == membersOnlyNote.Id);
        Assert.DoesNotContain(capturedSources, s => s.ArticleId == privateNote.Id);

        var persisted = await _context.Sessions.FindAsync(sessionId);
        Assert.NotNull(persisted);
        Assert.Equal("Generated summary", persisted!.AiSummary);
        Assert.Equal(TestHelpers.FixedIds.User2, persisted.AiSummaryGeneratedByUserId);
        Assert.NotNull(persisted.AiSummaryGeneratedAt);
    }

    [Fact]
    public async Task DeleteSessionAsync_Gm_DeletesSessionAndSessionLinkedChildContent()
    {
        var sessionId = Guid.NewGuid();
        var rootNoteId = Guid.NewGuid();
        var childArticleId = Guid.NewGuid();
        var unrelatedArticleId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var linkedQuestUpdateId = Guid.NewGuid();
        var unrelatedQuestUpdateId = Guid.NewGuid();

        var gm = await _context.Users.FindAsync(TestHelpers.FixedIds.User1);
        Assert.NotNull(gm);

        var session = new Session
        {
            Id = sessionId,
            ArcId = TestHelpers.FixedIds.Arc1,
            Name = "Delete Me",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };

        var rootNote = TestHelpers.CreateArticle(
            id: rootNoteId,
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User1,
            title: "Session Root Note",
            slug: "session-root-note",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);
        rootNote.SessionId = sessionId;

        var childArticle = TestHelpers.CreateArticle(
            id: childArticleId,
            worldId: TestHelpers.FixedIds.World1,
            parentId: rootNoteId,
            createdBy: TestHelpers.FixedIds.User1,
            title: "Child Content",
            slug: "child-content",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Public,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);

        var unrelatedArticle = TestHelpers.CreateArticle(
            id: unrelatedArticleId,
            worldId: TestHelpers.FixedIds.World1,
            createdBy: TestHelpers.FixedIds.User1,
            title: "Keep Me",
            slug: "keep-me",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Public,
            campaignId: TestHelpers.FixedIds.Campaign1,
            arcId: TestHelpers.FixedIds.Arc1);

        var quest = new Quest
        {
            Id = questId,
            ArcId = TestHelpers.FixedIds.Arc1,
            Title = "Tracked Quest",
            Status = QuestStatus.Active,
            IsGmOnly = false,
            SortOrder = 1,
            CreatedBy = TestHelpers.FixedIds.User1,
            Creator = gm!,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }
        };

        var linkedQuestUpdate = new QuestUpdate
        {
            Id = linkedQuestUpdateId,
            QuestId = questId,
            SessionId = sessionId,
            Body = "Happened in deleted session",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };

        var unrelatedQuestUpdate = new QuestUpdate
        {
            Id = unrelatedQuestUpdateId,
            QuestId = questId,
            Body = "Keep this update",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };

        var outgoingLink = new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = rootNoteId,
            TargetArticleId = unrelatedArticleId,
            Position = 0,
            CreatedAt = DateTime.UtcNow
        };

        var incomingLink = new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = unrelatedArticleId,
            TargetArticleId = childArticleId,
            Position = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        _context.Articles.AddRange(rootNote, childArticle, unrelatedArticle);
        _context.Quests.Add(quest);
        _context.QuestUpdates.AddRange(linkedQuestUpdate, unrelatedQuestUpdate);
        _context.ArticleLinks.AddRange(outgoingLink, incomingLink);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteSessionAsync(sessionId, TestHelpers.FixedIds.User1);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.True(result.Value);

        Assert.Null(await _context.Sessions.FindAsync(sessionId));
        Assert.Null(await _context.Articles.FindAsync(rootNoteId));
        Assert.Null(await _context.Articles.FindAsync(childArticleId));
        Assert.NotNull(await _context.Articles.FindAsync(unrelatedArticleId));

        Assert.Empty(await _context.ArticleLinks.ToListAsync());

        var remainingQuestUpdates = await _context.QuestUpdates
            .OrderBy(qu => qu.Id)
            .ToListAsync();

        Assert.Single(remainingQuestUpdates);
        Assert.Equal(unrelatedQuestUpdateId, remainingQuestUpdates[0].Id);

        await _worldDocumentService.Received(1).DeleteArticleImagesAsync(rootNoteId);
        await _worldDocumentService.Received(1).DeleteArticleImagesAsync(childArticleId);
    }

    [Fact]
    public void BuildDefaultNoteTitle_PrivateHelper_CoversWhitespaceAndTruncation()
    {
        var method = typeof(SessionService).GetMethod("BuildDefaultNoteTitle", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var defaultTitle = (string)method!.Invoke(null, new object?[] { "   " })!;
        Assert.Equal("My Notes", defaultTitle);

        var nullTitle = (string)method.Invoke(null, new object?[] { null })!;
        Assert.Equal("My Notes", nullTitle);

        var longUser = new string('a', 600);
        var longTitle = (string)method.Invoke(null, new object?[] { longUser })!;
        Assert.True(longTitle.Length <= 500);
        Assert.StartsWith("a", longTitle, StringComparison.Ordinal);
    }

    private void SeedTestData()
    {
        var (world, gm, player) = TestHelpers.SeedBasicWorld(_context);

        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id,
            name: "Test Campaign");
        campaign.OwnerId = gm.Id;
        campaign.Owner = gm;
        _context.Campaigns.Add(campaign);

        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id,
            name: "Act 1");
        arc.CreatedBy = gm.Id;
        arc.Creator = gm;
        _context.Arcs.Add(arc);

        _context.SaveChanges();
    }
}
