using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ExportServiceBranchCoverageTests
{
    [Fact]
    public void ExportService_PrivateHelpers_CoverRemainingBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var membership = Substitute.For<IWorldMembershipService>();
        var service = new ExportService(db, membership, NullLogger<ExportService>.Instance);

        var buildArticleMarkdown = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "BuildArticleMarkdown");
        var buildCampaignMarkdown = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "BuildCampaignMarkdown");
        var buildArcMarkdown = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "BuildArcMarkdown");
        var htmlToMarkdown = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "HtmlToMarkdown");
        var processList = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "ProcessList");
        var sanitizeFileName = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "SanitizeFileName");
        var escapeYaml = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ExportService), "EscapeYaml");

        var articleWithAllFields = new Article
        {
            Title = "Title",
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            SessionDate = DateTime.UtcNow.Date,
            InGameDate = "1492 DR",
            IconEmoji = "🔥",
            Body = "<p>Body</p>",
            AISummary = "Summary",
            AISummaryGeneratedAt = DateTime.UtcNow
        };

        var markdownWithAll = (string)buildArticleMarkdown.Invoke(service, [articleWithAllFields])!;
        Assert.Contains("modified:", markdownWithAll);
        Assert.Contains("session_date:", markdownWithAll);
        Assert.Contains("in_game_date:", markdownWithAll);
        Assert.Contains("icon:", markdownWithAll);
        Assert.Contains("## AI Summary", markdownWithAll);
        Assert.Contains("Generated:", markdownWithAll);

        var articleMinimal = new Article
        {
            Title = "Minimal",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };
        var markdownMinimal = (string)buildArticleMarkdown.Invoke(service, [articleMinimal])!;
        Assert.DoesNotContain("modified:", markdownMinimal);
        Assert.DoesNotContain("session_date:", markdownMinimal);

        var campaignWithDescription = new Campaign { Name = "C", CreatedAt = DateTime.UtcNow, Description = "desc" };
        var campaignWithoutDescription = new Campaign { Name = "C2", CreatedAt = DateTime.UtcNow };
        Assert.Contains("desc", (string)buildCampaignMarkdown.Invoke(service, [campaignWithDescription])!);
        Assert.DoesNotContain("desc", (string)buildCampaignMarkdown.Invoke(service, [campaignWithoutDescription])!);

        var arcWithDescription = new Arc { Name = "A", CreatedAt = DateTime.UtcNow, Description = "arc", SortOrder = 1 };
        var arcWithoutDescription = new Arc { Name = "A2", CreatedAt = DateTime.UtcNow, SortOrder = 1 };
        Assert.Contains("arc", (string)buildArcMarkdown.Invoke(service, [arcWithDescription])!);
        Assert.DoesNotContain("arc", (string)buildArcMarkdown.Invoke(service, [arcWithoutDescription])!);

        var complexHtml = @"<h1>Header</h1>
<p>Text with <strong>bold</strong> and <i>italic</i></p>
<ul><li>one</li><li>two<ul><li>inner</li></ul></li><li>broken<ul></li></ul></li></ul>
<ol><li>first</li><li>second<ol><li>inner ordered</li></ol></li></ol>
<blockquote><p>quote</p></blockquote>";
        var complexMarkdown = (string)htmlToMarkdown.Invoke(service, [complexHtml])!;
        Assert.Contains("# Header", complexMarkdown);
        Assert.Contains("- one", complexMarkdown);
        Assert.Contains("1. first", complexMarkdown);

        Assert.Equal(string.Empty, (string)htmlToMarkdown.Invoke(service, ["  "])!);
        _ = (string)processList.Invoke(service, ["<li>plain</li>", false, 0])!;
        _ = (string)processList.Invoke(service, ["<li>parent<ul>child</ul></li>", false, 0])!;
        _ = (string)processList.Invoke(service, ["<li>parent<ol>child</ol></li>", false, 0])!;
        _ = (string)processList.Invoke(service, ["<li>broken<ul>no close</li>", false, 0])!;

        Assert.Equal("Untitled", (string)sanitizeFileName.Invoke(null, [""])!);
        var longName = new string('a', 150);
        var sanitizedLong = (string)sanitizeFileName.Invoke(null, [longName])!;
        Assert.Equal(100, sanitizedLong.Length);
        Assert.Equal("name", (string)sanitizeFileName.Invoke(null, ["name"])!);

        Assert.Equal(string.Empty, (string)escapeYaml.Invoke(null, [""])!);
        var escaped = (string)escapeYaml.Invoke(null, ["a\\b\"c\n\r"])!;
        Assert.Contains("\\\\", escaped);
        Assert.Contains("\\\"", escaped);
    }
}
