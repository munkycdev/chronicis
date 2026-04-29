using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Tests for the Phase 09 slug backfill logic
/// (UrlRestructure_SessionNoteSlugScope migration).
///
/// EF InMemory does not execute raw SQL, so the backfill logic is re-implemented
/// here as a helper that mirrors the migration's T-SQL algorithm.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionNoteSlugMigrationTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private bool _disposed;

    public SessionNoteSlugMigrationTests()
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
        if (_disposed) return;
        if (disposing) _context.Dispose();
        _disposed = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Backfill simulation — mirrors the migration's T-SQL logic
    // ─────────────────────────────────────────────────────────────────────────

    private static bool IsGuidShaped(string slug) =>
        slug.Length == 36 &&
        slug[8] == '-' && slug[13] == '-' && slug[18] == '-' && slug[23] == '-' &&
        slug.Replace("-", "").Length == 32;

    private static string DeriveSlug(string? title)
    {
        // Mirrors the T-SQL: lowercase, strip apostrophes, replace non-[a-z0-9] with hyphens,
        // collapse runs, trim, fallback to "note" (not "untitled" as SlugGenerator uses).
        var slug = SlugGenerator.GenerateSlug(title ?? string.Empty);
        return slug == "untitled" ? "note" : slug;
    }

    /// <summary>
    /// Simulates the Phase 09 migration backfill on the in-memory context.
    /// </summary>
    private async Task RunBackfillAsync()
    {
        // Pass 1: derive slugs for GUID-shaped session notes
        var guidshaped = await _context.Articles
            .Where(a => a.Type == ArticleType.SessionNote &&
                        a.SessionId != null &&
                        a.Slug.Length == 36)
            .ToListAsync();

        foreach (var note in guidshaped.Where(n => IsGuidShaped(n.Slug)))
        {
            note.Slug = DeriveSlug(note.Title);
        }

        await _context.SaveChangesAsync();

        // Pass 2: resolve within-session collisions ordered by CreatedAt
        var sessionNotes = await _context.Articles
            .Where(a => a.Type == ArticleType.SessionNote && a.SessionId != null)
            .OrderBy(a => a.SessionId)
            .ThenBy(a => a.Slug)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();

        var groups = sessionNotes
            .GroupBy(a => new { a.SessionId, a.Slug })
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(a => a.CreatedAt).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                ordered[i].Slug = group.Key.Slug + "-" + (i + 1);
            }
        }

        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Article MakeNote(Guid sessionId, string title, string slug, DateTime? createdAt = null)
    {
        var note = new Article
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Type = ArticleType.SessionNote,
            Visibility = ArticleVisibility.Public,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow
        };
        note.SessionId = sessionId;
        return note;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Backfill_GuidSlug_IsReplacedWithTitleDerivedSlug()
    {
        var sessionId = Guid.NewGuid();
        var guidSlug = "cc973118-fd4b-4118-9407-5dab94c4fb34";
        var note = MakeNote(sessionId, "Munky's Notes", guidSlug);
        _context.Articles.Add(note);
        await _context.SaveChangesAsync();

        await RunBackfillAsync();

        var updated = await _context.Articles.FindAsync(note.Id);
        Assert.Equal("munkys-notes", updated!.Slug);
    }

    [Fact]
    public async Task Backfill_NonGuidSlug_IsNotChanged()
    {
        var sessionId = Guid.NewGuid();
        var note = MakeNote(sessionId, "Session Notes", "session-notes");
        _context.Articles.Add(note);
        await _context.SaveChangesAsync();

        await RunBackfillAsync();

        var updated = await _context.Articles.FindAsync(note.Id);
        Assert.Equal("session-notes", updated!.Slug);
    }

    [Fact]
    public async Task Backfill_WithinSessionCollision_ResolvesWithSuffix()
    {
        var sessionId = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var note1 = MakeNote(sessionId, "My Notes", "aa000000-0000-0000-0000-000000000001", t);
        var note2 = MakeNote(sessionId, "My Notes", "aa000000-0000-0000-0000-000000000002", t.AddSeconds(1));
        _context.Articles.AddRange(note1, note2);
        await _context.SaveChangesAsync();

        await RunBackfillAsync();

        var updated1 = await _context.Articles.FindAsync(note1.Id);
        var updated2 = await _context.Articles.FindAsync(note2.Id);

        // Earliest keeps base slug; later gets suffix
        Assert.Equal("my-notes", updated1!.Slug);
        Assert.Equal("my-notes-2", updated2!.Slug);
    }

    [Fact]
    public async Task Backfill_AcrossSessionsSameTitle_SlugIsUnchangedPerSession()
    {
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var note1 = MakeNote(sessionId1, "GM Notes", "bb000000-0000-0000-0000-000000000001");
        var note2 = MakeNote(sessionId2, "GM Notes", "bb000000-0000-0000-0000-000000000002");
        _context.Articles.AddRange(note1, note2);
        await _context.SaveChangesAsync();

        await RunBackfillAsync();

        var updated1 = await _context.Articles.FindAsync(note1.Id);
        var updated2 = await _context.Articles.FindAsync(note2.Id);

        // Same derived slug is allowed in different sessions
        Assert.Equal("gm-notes", updated1!.Slug);
        Assert.Equal("gm-notes", updated2!.Slug);
    }

    [Fact]
    public async Task Backfill_EmptyTitle_FallsBackToNote()
    {
        var sessionId = Guid.NewGuid();
        var note = MakeNote(sessionId, "", "cc000000-0000-0000-0000-000000000001");
        _context.Articles.Add(note);
        await _context.SaveChangesAsync();

        await RunBackfillAsync();

        var updated = await _context.Articles.FindAsync(note.Id);
        Assert.Equal("note", updated!.Slug);
    }

    [Fact]
    public async Task Backfill_IsIdempotent_RerunDoesNotDoubleSuffix()
    {
        var sessionId = Guid.NewGuid();
        var note = MakeNote(sessionId, "My Notes", "dd000000-0000-0000-0000-000000000001");
        _context.Articles.Add(note);
        await _context.SaveChangesAsync();

        // First run
        await RunBackfillAsync();
        var afterFirst = (await _context.Articles.FindAsync(note.Id))!.Slug;

        // Second run — should be a no-op
        await RunBackfillAsync();
        var afterSecond = (await _context.Articles.FindAsync(note.Id))!.Slug;

        Assert.Equal(afterFirst, afterSecond);
    }
}
