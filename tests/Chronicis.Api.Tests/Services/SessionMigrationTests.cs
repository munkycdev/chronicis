using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Tests that validate Phase 1 migration invariants for the Session entity refactor.
///
/// Because EF Core's InMemoryDatabase does not execute raw SQL migrations,
/// these tests simulate the pre-migration state and execute the same
/// backfill logic as a tested helper to assert mapping correctness.
/// This matches the established test pattern in this codebase.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionMigrationTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private bool _disposed;

    public SessionMigrationTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
    }

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

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers — simulated backfill (mirrors migration SQL logic)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulates the migration backfill: creates Session entities from legacy Session articles
    /// and wires SessionNote articles via SessionId.
    /// Returns a dictionary mapping legacy Article.Id → new Session.Id.
    /// </summary>
    private async Task<Dictionary<Guid, Guid>> RunSessionBackfillAsync()
    {
        // Step 1: migrate legacy Session articles → Session entities (Ids preserved)
        var legacySessionArticles = await _context.Articles
            .Where(a => a.Type == ArticleType.Session && a.ArcId != null)
            .ToListAsync();

        var mapping = new Dictionary<Guid, Guid>();

        foreach (var article in legacySessionArticles)
        {
            var session = new Session
            {
                Id = article.Id,   // preserve Id
                ArcId = article.ArcId!.Value,
                Name = article.Title,
                SessionDate = article.SessionDate,
                PublicNotes = article.Body,
                PrivateNotes = null,
                AiSummary = article.AISummary,
                AiSummaryGeneratedAt = article.AISummaryGeneratedAt,
                AiSummaryGeneratedByUserId = null,
                CreatedAt = article.CreatedAt,
                ModifiedAt = article.ModifiedAt,  // nullable; null if never modified
                CreatedBy = article.CreatedBy
            };
            _context.Sessions.Add(session);
            mapping[article.Id] = session.Id;
        }

        await _context.SaveChangesAsync();

        // Step 2: reattach SessionNote children
        var sessionNotes = await _context.Articles
            .Where(a => a.Type == ArticleType.SessionNote && a.ParentId != null)
            .ToListAsync();

        foreach (var note in sessionNotes)
        {
            if (note.ParentId.HasValue && mapping.ContainsKey(note.ParentId.Value))
            {
                note.SessionId = note.ParentId.Value;
            }
        }

        await _context.SaveChangesAsync();

        // Step 3 (Phase 1 bridge backfill) is removed in Phase 7 because QuestUpdate.SessionId
        // is now the canonical Session FK. The simulated migration helper keeps the legacy mapping
        // assertions focused on the SessionId value itself.

        return mapping;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Seed helpers
    // ─────────────────────────────────────────────────────────────────────────

    private (User gm, Arc arc, Guid campaignId) SeedArcWithUser()
    {
        var gm = TestHelpers.CreateUser(id: Guid.NewGuid(), displayName: "GM User");
        var world = TestHelpers.CreateWorld(ownerId: gm.Id);
        var campaign = TestHelpers.CreateCampaign(worldId: world.Id);
        campaign.OwnerId = gm.Id;

        var arc = TestHelpers.CreateArc(campaignId: campaign.Id);
        arc.CreatedBy = gm.Id;

        _context.Users.Add(gm);
        _context.Worlds.Add(world);
        _context.Campaigns.Add(campaign);
        _context.Arcs.Add(arc);
        _context.SaveChanges();

        return (gm, arc, campaign.Id);
    }

    private Article CreateLegacySessionArticle(Guid arcId, Guid campaignId, Guid createdBy,
        string title = "Session 1", string? body = "Public content")
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            ArcId = arcId,
            CampaignId = campaignId,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Body = body,
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Public,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow
        };
    }

    private Article CreateSessionNoteArticle(Guid parentId, Guid arcId, Guid campaignId, Guid createdBy,
        string title = "Player Notes", ArticleVisibility visibility = ArticleVisibility.Public)
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            ArcId = arcId,
            CampaignId = campaignId,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Body = "Note body",
            Type = ArticleType.SessionNote,
            Visibility = visibility,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Empty DB safety
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_OnEmptyDatabase_ProducesNoSessions()
    {
        var mapping = await RunSessionBackfillAsync();

        Assert.Empty(mapping);
        Assert.Empty(await _context.Sessions.ToListAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  1-to-1 Session creation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_EachLegacySessionArticle_CreatesExactlyOneSessionEntity()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        var s1 = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id, "Session 1");
        var s2 = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id, "Session 2");
        _context.Articles.AddRange(s1, s2);
        await _context.SaveChangesAsync();

        var mapping = await RunSessionBackfillAsync();

        Assert.Equal(2, mapping.Count);
        var sessions = await _context.Sessions.ToListAsync();
        Assert.Equal(2, sessions.Count);

        // Ids preserved
        Assert.Contains(sessions, s => s.Id == s1.Id);
        Assert.Contains(sessions, s => s.Id == s2.Id);
    }

    [Fact]
    public async Task Backfill_Session_InheritsFieldsFromLegacyArticle()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        var sessionDate = DateTime.UtcNow.Date;
        var article = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id, "The Dark Forest", "GM public notes");
        article.SessionDate = sessionDate;
        article.AISummary = "AI generated";
        article.AISummaryGeneratedAt = DateTime.UtcNow;
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var session = await _context.Sessions.SingleAsync(s => s.Id == article.Id);
        Assert.Equal("The Dark Forest", session.Name);
        Assert.Equal("GM public notes", session.PublicNotes);
        Assert.Null(session.PrivateNotes);
        Assert.Equal(sessionDate, session.SessionDate);
        Assert.Equal(article.ArcId, session.ArcId);
        Assert.Equal(gm.Id, session.CreatedBy);
        Assert.Equal("AI generated", session.AiSummary);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SessionNote reattachment
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_AllSessionNotes_GetSessionIdSet()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        var sessionArticle = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id);
        _context.Articles.Add(sessionArticle);
        await _context.SaveChangesAsync();

        var note1 = CreateSessionNoteArticle(sessionArticle.Id, arc.Id, campaignId, gm.Id, "GM Notes");
        var note2 = CreateSessionNoteArticle(sessionArticle.Id, arc.Id, campaignId, gm.Id, "Player Notes");
        _context.Articles.AddRange(note1, note2);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var updatedNote1 = await _context.Articles.FindAsync(note1.Id);
        var updatedNote2 = await _context.Articles.FindAsync(note2.Id);

        Assert.Equal(sessionArticle.Id, updatedNote1!.SessionId);
        Assert.Equal(sessionArticle.Id, updatedNote2!.SessionId);
    }

    [Fact]
    public async Task Backfill_NoSessionNotesAreLost()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        var sessionArticle = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id);
        _context.Articles.Add(sessionArticle);
        await _context.SaveChangesAsync();

        var note1 = CreateSessionNoteArticle(sessionArticle.Id, arc.Id, campaignId, gm.Id, "Note A", ArticleVisibility.Public);
        var note2 = CreateSessionNoteArticle(sessionArticle.Id, arc.Id, campaignId, gm.Id, "Note B", ArticleVisibility.MembersOnly);
        var note3 = CreateSessionNoteArticle(sessionArticle.Id, arc.Id, campaignId, gm.Id, "Note C", ArticleVisibility.Private);
        _context.Articles.AddRange(note1, note2, note3);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var sessionNotes = await _context.Articles
            .Where(a => a.Type == ArticleType.SessionNote)
            .ToListAsync();

        // All 3 survive regardless of visibility
        Assert.Equal(3, sessionNotes.Count);
        Assert.All(sessionNotes, n => Assert.Equal(sessionArticle.Id, n.SessionId));
    }

    [Fact]
    public async Task Backfill_WikiArticleChildren_DoNotGetSessionId()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        // A WikiArticle that happens to be a child of a session article (edge case)
        var sessionArticle = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id);
        _context.Articles.Add(sessionArticle);
        await _context.SaveChangesAsync();

        var wikiChild = TestHelpers.CreateArticle(
            parentId: sessionArticle.Id,
            worldId: Guid.NewGuid(),
            createdBy: gm.Id,
            type: ArticleType.WikiArticle);
        _context.Articles.Add(wikiChild);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var updated = await _context.Articles.FindAsync(wikiChild.Id);
        Assert.Null(updated!.SessionId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  QuestUpdate bridge
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_QuestUpdates_GetSessionEntityIdPopulated()
    {
        var (gm, arc, campaignId) = SeedArcWithUser();

        var sessionArticle = CreateLegacySessionArticle(arc.Id, campaignId, gm.Id);
        _context.Articles.Add(sessionArticle);

        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Title = "Find the artifact",
            CreatedBy = gm.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Quests.Add(quest);
        await _context.SaveChangesAsync();

        var update = new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = quest.Id,
            SessionId = sessionArticle.Id,   // legacy FK
            Body = "Party advanced the quest",
            CreatedBy = gm.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.QuestUpdates.Add(update);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var updated = await _context.QuestUpdates.FindAsync(update.Id);
        Assert.NotNull(updated);
        Assert.Equal(sessionArticle.Id, updated!.SessionId);
    }

    [Fact]
    public async Task Backfill_QuestUpdateWithNullSessionId_LeavesSessionIdNull()
    {
        var (gm, arc, _) = SeedArcWithUser();

        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Title = "A quest",
            CreatedBy = gm.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Quests.Add(quest);
        await _context.SaveChangesAsync();

        var update = new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = quest.Id,
            SessionId = null,
            Body = "No session reference",
            CreatedBy = gm.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.QuestUpdates.Add(update);
        await _context.SaveChangesAsync();

        await RunSessionBackfillAsync();

        var updated = await _context.QuestUpdates.FindAsync(update.Id);
        Assert.Null(updated!.SessionId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Orphaned legacy Session articles (null ArcId) must be skipped
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_LegacySessionArticle_WithNullArcId_IsSkipped()
    {
        var gm = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(ownerId: gm.Id);
        _context.Users.Add(gm);
        _context.Worlds.Add(world);
        await _context.SaveChangesAsync();

        // Orphaned session article (no ArcId)
        var orphan = new Article
        {
            Id = Guid.NewGuid(),
            ArcId = null,
            Title = "Orphan Session",
            Slug = "orphan-session",
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Public,
            CreatedBy = gm.Id,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow
        };
        _context.Articles.Add(orphan);
        await _context.SaveChangesAsync();

        var mapping = await RunSessionBackfillAsync();

        Assert.Empty(mapping);
        Assert.Empty(await _context.Sessions.ToListAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Article model: SessionId field
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Article_SessionId_DefaultsToNull()
    {
        var article = new Article();
        Assert.Null(article.SessionId);
    }

    [Fact]
    public void Article_SessionId_CanBeSet()
    {
        var sessionId = Guid.NewGuid();
        var article = new Article { SessionId = sessionId };
        Assert.Equal(sessionId, article.SessionId);
    }
}
