using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class ExportServiceTests
{
    private readonly ExportService _sut;

    public ExportServiceTests()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var membership = Substitute.For<IWorldMembershipService>();
        _sut = new ExportService(db, membership, NullLogger<ExportService>.Instance);
    }

    // ── BuildArticleMarkdown ────────────────────────────────

    [Fact]
    public void BuildArticleMarkdown_IncludesAllOptionalFields()
    {
        var article = new Article
        {
            Title = "Session 1",
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            SessionDate = DateTime.UtcNow.Date,
            InGameDate = "1492 DR",
            IconEmoji = "🔥",
            Body = "<p>Body</p>",
            AISummary = "Summary text",
            AISummaryGeneratedAt = DateTime.UtcNow
        };

        var result = _sut.BuildArticleMarkdown(article);

        Assert.Contains("title: \"Session 1\"", result);
        Assert.Contains("type: Session", result);
        Assert.Contains("visibility: Public", result);
        Assert.Contains("modified:", result);
        Assert.Contains("session_date:", result);
        Assert.Contains("in_game_date: \"1492 DR\"", result);
        Assert.Contains("icon: \"🔥\"", result);
        Assert.Contains("# Session 1", result);
        Assert.Contains("Body", result);
        Assert.Contains("## AI Summary", result);
        Assert.Contains("Summary text", result);
        Assert.Contains("Generated:", result);
    }

    [Fact]
    public void BuildArticleMarkdown_OmitsOptionalFields_WhenNull()
    {
        var article = new Article
        {
            Title = "Minimal",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };

        var result = _sut.BuildArticleMarkdown(article);

        Assert.Contains("title: \"Minimal\"", result);
        Assert.DoesNotContain("modified:", result);
        Assert.DoesNotContain("session_date:", result);
        Assert.DoesNotContain("in_game_date:", result);
        Assert.DoesNotContain("icon:", result);
        Assert.DoesNotContain("AI Summary", result);
    }

    [Fact]
    public void BuildArticleMarkdown_IncludesAISummary_WithoutTimestamp()
    {
        var article = new Article
        {
            Title = "X",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            AISummary = "AI text"
        };

        var result = _sut.BuildArticleMarkdown(article);
        Assert.Contains("## AI Summary", result);
        Assert.DoesNotContain("Generated:", result);
    }

    // ── BuildCampaignMarkdown ───────────────────────────────

    [Fact]
    public void BuildCampaignMarkdown_WithDescription()
    {
        var campaign = new Campaign { Name = "Campaign", CreatedAt = DateTime.UtcNow, Description = "desc" };
        var result = _sut.BuildCampaignMarkdown(campaign);

        Assert.Contains("title: \"Campaign\"", result);
        Assert.Contains("type: Campaign", result);
        Assert.Contains("# Campaign", result);
        Assert.Contains("desc", result);
    }

    [Fact]
    public void BuildCampaignMarkdown_WithoutDescription()
    {
        var campaign = new Campaign { Name = "C2", CreatedAt = DateTime.UtcNow };
        var result = _sut.BuildCampaignMarkdown(campaign);
        Assert.DoesNotContain("desc", result);
    }

    // ── BuildArcMarkdown ────────────────────────────────────

    [Fact]
    public void BuildArcMarkdown_WithDescription()
    {
        var arc = new Arc { Name = "Arc1", CreatedAt = DateTime.UtcNow, Description = "arc desc", SortOrder = 1 };
        var result = _sut.BuildArcMarkdown(arc);

        Assert.Contains("title: \"Arc1\"", result);
        Assert.Contains("type: Arc", result);
        Assert.Contains("sort_order: 1", result);
        Assert.Contains("# Arc1", result);
        Assert.Contains("arc desc", result);
    }

    [Fact]
    public void BuildArcMarkdown_WithoutDescription()
    {
        var arc = new Arc { Name = "A2", CreatedAt = DateTime.UtcNow, SortOrder = 2 };
        var result = _sut.BuildArcMarkdown(arc);
        Assert.DoesNotContain("desc", result);
    }

    [Fact]
    public void BuildSessionMarkdown_IncludesModifiedAndAiSummaryTimestamp()
    {
        var session = new Session
        {
            Name = "Session X",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ModifiedAt = DateTime.UtcNow,
            SessionDate = DateTime.UtcNow.Date,
            PublicNotes = "<p>Body</p>",
            AiSummary = "summary",
            AiSummaryGeneratedAt = DateTime.UtcNow
        };

        var result = _sut.BuildSessionMarkdown(session);

        Assert.Contains("modified:", result);
        Assert.Contains("session_date:", result);
        Assert.Contains("## AI Summary", result);
        Assert.Contains("*Generated:", result);
    }

    // ── SanitizeFileName ────────────────────────────────────

    [Fact]
    public void SanitizeFileName_ReturnsUntitled_ForEmpty()
    {
        Assert.Equal("Untitled", ExportService.SanitizeFileName(""));
        Assert.Equal("Untitled", ExportService.SanitizeFileName("  "));
    }

    [Fact]
    public void SanitizeFileName_ReplacesInvalidChars()
    {
        var result = ExportService.SanitizeFileName("a/b\\c:d");
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
        Assert.DoesNotContain(":", result);
    }

    [Fact]
    public void SanitizeFileName_TruncatesAt100()
    {
        var longName = new string('a', 150);
        Assert.Equal(100, ExportService.SanitizeFileName(longName).Length);
    }

    [Fact]
    public void SanitizeFileName_PreservesValidNames()
    {
        Assert.Equal("valid name", ExportService.SanitizeFileName("valid name"));
    }

    [Fact]
    public void GetUniqueSiblingName_SkipsExistingSuffixes_UntilFreeName()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name",
            "Name (2)"
        };

        var actual = ExportService.GetUniqueSiblingName("Name", used);

        Assert.Equal("Name (3)", actual);
    }

    // ── EscapeYaml ──────────────────────────────────────────

    [Fact]
    public void EscapeYaml_ReturnsEmpty_ForEmpty()
    {
        Assert.Equal("", ExportService.EscapeYaml(""));
    }

    [Fact]
    public void EscapeYaml_EscapesSpecialChars()
    {
        var result = ExportService.EscapeYaml("a\\b\"c\n\r");
        Assert.Contains("\\\\", result);
        Assert.Contains("\\\"", result);
        Assert.Contains("\\n", result);
        Assert.DoesNotContain("\r", result);
    }

    // ── ExportWorldToMarkdownAsync (Phase 4) ──────────────────────

    [Fact]
    public async Task ExportWorldToMarkdownAsync_ExportsSessionFoldersAndSessionNotes()
    {
        using var db = CreateDbContext();
        var membership = Substitute.For<IWorldMembershipService>();
        var sut = new ExportService(db, membership, NullLogger<ExportService>.Instance);

        var worldId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        membership.UserHasAccessAsync(worldId, userId).Returns(true);

        var (worldName, campaignName, arcName, sessionName) = await SeedSessionExportScenarioAsync(db, worldId);

        var bytes = await sut.ExportWorldToMarkdownAsync(worldId, userId);

        Assert.NotNull(bytes);

        var entries = ReadArchiveEntries(bytes!);
        var basePath = $"{ExportService.SanitizeFileName(worldName)}/Campaigns/{ExportService.SanitizeFileName(campaignName)}/{ExportService.SanitizeFileName(arcName)}/{ExportService.SanitizeFileName(sessionName)}";

        Assert.Contains($"{basePath}/{ExportService.SanitizeFileName(sessionName)}.md", entries.Keys);
        Assert.Contains($"{basePath}/Player Notes.md", entries.Keys);
    }

    [Fact]
    public async Task ExportWorldToMarkdownAsync_SessionMarkdown_ExcludesPrivateNotes_ButIncludesPublicNotesAndAiSummary()
    {
        using var db = CreateDbContext();
        var membership = Substitute.For<IWorldMembershipService>();
        var sut = new ExportService(db, membership, NullLogger<ExportService>.Instance);

        var worldId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        membership.UserHasAccessAsync(worldId, userId).Returns(true);

        var (worldName, campaignName, arcName, sessionName) = await SeedSessionExportScenarioAsync(db, worldId);

        var bytes = await sut.ExportWorldToMarkdownAsync(worldId, userId);

        Assert.NotNull(bytes);

        var entries = ReadArchiveEntries(bytes!);
        var sessionPath = $"{ExportService.SanitizeFileName(worldName)}/Campaigns/{ExportService.SanitizeFileName(campaignName)}/{ExportService.SanitizeFileName(arcName)}/{ExportService.SanitizeFileName(sessionName)}/{ExportService.SanitizeFileName(sessionName)}.md";
        var sessionMarkdown = entries[sessionPath];

        Assert.Contains("PUBLIC_EXPORT_TOKEN", sessionMarkdown);
        Assert.Contains("SESSION_AI_SUMMARY_TOKEN", sessionMarkdown);
        Assert.DoesNotContain("PRIVATE_EXPORT_TOKEN", sessionMarkdown);
    }

    [Fact]
    public async Task ExportWorldToMarkdownAsync_RenamesDuplicateSessionAndNoteFileNames_WithNumericSuffixes()
    {
        using var db = CreateDbContext();
        var membership = Substitute.For<IWorldMembershipService>();
        var sut = new ExportService(db, membership, NullLogger<ExportService>.Instance);

        var worldId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        membership.UserHasAccessAsync(worldId, userId).Returns(true);

        var world = new World
        {
            Id = worldId,
            Name = "Collision World",
            Slug = "collision-world",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Name = "Campaign",
            OwnerId = world.OwnerId,
            CreatedAt = DateTime.UtcNow
        };
        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Name = "Arc",
            SortOrder = 1,
            CreatedBy = world.OwnerId,
            CreatedAt = DateTime.UtcNow
        };

        var firstSession = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Same Session",
            PublicNotes = "<p>S1</p>",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            CreatedBy = world.OwnerId
        };
        var secondSession = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Same Session",
            PublicNotes = "<p>S2</p>",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedBy = world.OwnerId
        };

        var note1 = CreateSessionNote(worldId, campaign.Id, arc.Id, firstSession.Id, "Duplicate Note", "N1", world.OwnerId, DateTime.UtcNow.AddMinutes(-2));
        var note2 = CreateSessionNote(worldId, campaign.Id, arc.Id, firstSession.Id, "Duplicate Note", "N2", world.OwnerId, DateTime.UtcNow.AddMinutes(-1));

        db.Worlds.Add(world);
        db.Campaigns.Add(campaign);
        db.Arcs.Add(arc);
        db.Sessions.AddRange(firstSession, secondSession);
        db.Articles.AddRange(note1, note2);
        await db.SaveChangesAsync();

        var bytes = await sut.ExportWorldToMarkdownAsync(worldId, userId);

        Assert.NotNull(bytes);

        var entries = ReadArchiveEntries(bytes!);
        var root = $"{ExportService.SanitizeFileName(world.Name)}/Campaigns/{ExportService.SanitizeFileName(campaign.Name)}/{ExportService.SanitizeFileName(arc.Name)}";

        Assert.Contains($"{root}/Same Session/Same Session.md", entries.Keys);
        Assert.Contains($"{root}/Same Session (2)/Same Session (2).md", entries.Keys);
        Assert.Contains($"{root}/Same Session/Duplicate Note.md", entries.Keys);
        Assert.Contains($"{root}/Same Session/Duplicate Note (2).md", entries.Keys);
    }

    private static ChronicisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase($"export-tests-{Guid.NewGuid()}")
            .Options;

        return new ChronicisDbContext(options);
    }

    private static async Task<(string WorldName, string CampaignName, string ArcName, string SessionName)> SeedSessionExportScenarioAsync(
        ChronicisDbContext db,
        Guid worldId)
    {
        var ownerId = Guid.NewGuid();
        var world = new World
        {
            Id = worldId,
            Name = "Export World",
            Slug = "export-world",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Name = "Campaign Alpha",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Name = "Arc One",
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ownerId
        };
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Session Prime",
            SessionDate = new DateTime(2026, 2, 24, 0, 0, 0, DateTimeKind.Utc),
            PublicNotes = "<p>PUBLIC_EXPORT_TOKEN</p>",
            PrivateNotes = "<p>PRIVATE_EXPORT_TOKEN</p>",
            AiSummary = "SESSION_AI_SUMMARY_TOKEN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = ownerId
        };

        var note = CreateSessionNote(
            world.Id,
            campaign.Id,
            arc.Id,
            session.Id,
            "Player Notes",
            "PLAYER_NOTE_EXPORT_TOKEN",
            ownerId,
            DateTime.UtcNow);

        db.Worlds.Add(world);
        db.Campaigns.Add(campaign);
        db.Arcs.Add(arc);
        db.Sessions.Add(session);
        db.Articles.Add(note);
        await db.SaveChangesAsync();

        return (world.Name, campaign.Name, arc.Name, session.Name);
    }

    private static Article CreateSessionNote(
        Guid worldId,
        Guid campaignId,
        Guid arcId,
        Guid sessionId,
        string title,
        string bodyToken,
        Guid createdBy,
        DateTime createdAt)
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            CampaignId = campaignId,
            ArcId = arcId,
            SessionId = sessionId,
            Title = title,
            Slug = Guid.NewGuid().ToString("N"),
            Body = $"<p>{bodyToken}</p>",
            Type = ArticleType.SessionNote,
            Visibility = ArticleVisibility.Public,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            EffectiveDate = createdAt
        };
    }

    private static Dictionary<string, string> ReadArchiveEntries(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            entries[entry.FullName] = reader.ReadToEnd();
        }

        return entries;
    }
}
