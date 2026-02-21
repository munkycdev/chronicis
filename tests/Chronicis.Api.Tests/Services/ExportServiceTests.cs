using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
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
}
