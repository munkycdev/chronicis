using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class ArticleExternalLinkServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleExternalLinkService _service;
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _articleId = Guid.NewGuid();

    public ArticleExternalLinkServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new ArticleExternalLinkService(
            _context,
            NullLogger<ArticleExternalLinkService>.Instance);
    }

    private bool _disposed;
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

    // ── Helpers ───────────────────────────────────────────────────

    private static string BuildExternalLinkSpan(
        string source, string id, string title) =>
        $@"<span data-type=""external-link"" data-source=""{source}"" data-id=""{id}"" data-title=""{title}"">text</span>";

    private async Task SeedExternalLinks(Guid articleId, params (string Source, string ExternalId, string Title)[] links)
    {
        foreach (var link in links)
        {
            _context.ArticleExternalLinks.Add(new ArticleExternalLink
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                Source = link.Source,
                ExternalId = link.ExternalId,
                DisplayTitle = link.Title
            });
        }
        await _context.SaveChangesAsync();
    }

    // ── SyncExternalLinksAsync ────────────────────────────────────

    [Fact]
    public async Task Sync_NullHtml_CreatesNoLinks()
    {
        await _service.SyncExternalLinksAsync(_articleId, null);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_EmptyHtml_CreatesNoLinks()
    {
        await _service.SyncExternalLinksAsync(_articleId, "");

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_HtmlWithNoExternalLinks_CreatesNoLinks()
    {
        var html = "<p>Just some regular content</p>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_SingleExternalLink_CreatesOneRecord()
    {
        var html = $"<p>Check out {BuildExternalLinkSpan("srd14", "spells/fireball", "Fireball")} for details.</p>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var links = await _context.ArticleExternalLinks.ToListAsync();
        Assert.Single(links);
        Assert.Equal(_articleId, links[0].ArticleId);
        Assert.Equal("srd14", links[0].Source);
        Assert.Equal("spells/fireball", links[0].ExternalId);
        Assert.Equal("Fireball", links[0].DisplayTitle);
    }

    [Fact]
    public async Task Sync_MultipleExternalLinks_CreatesAll()
    {
        var html = $@"
            <p>{BuildExternalLinkSpan("srd14", "spells/fireball", "Fireball")}</p>
            <p>{BuildExternalLinkSpan("open5e", "classes/wizard", "Wizard")}</p>
            <p>{BuildExternalLinkSpan("srd14", "monsters/dragon-red", "Red Dragon")}</p>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var links = await _context.ArticleExternalLinks.ToListAsync();
        Assert.Equal(3, links.Count);
    }

    [Fact]
    public async Task Sync_ReplacesExistingLinks()
    {
        // Seed an existing link
        await SeedExternalLinks(_articleId, ("srd14", "spells/magic-missile", "Magic Missile"));

        // Sync with different content
        var html = BuildExternalLinkSpan("open5e", "classes/wizard", "Wizard");
        await _service.SyncExternalLinksAsync(_articleId, html);

        var links = await _context.ArticleExternalLinks.ToListAsync();
        Assert.Single(links);
        Assert.Equal("open5e", links[0].Source);
        Assert.Equal("Wizard", links[0].DisplayTitle);
    }

    [Fact]
    public async Task Sync_EmptyHtml_RemovesExistingLinks()
    {
        await SeedExternalLinks(_articleId, ("srd14", "spells/fireball", "Fireball"));

        await _service.SyncExternalLinksAsync(_articleId, "");

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_DoesNotAffectOtherArticles()
    {
        var otherArticleId = Guid.NewGuid();
        await SeedExternalLinks(otherArticleId, ("srd14", "spells/fireball", "Fireball"));

        var html = BuildExternalLinkSpan("open5e", "classes/wizard", "Wizard");
        await _service.SyncExternalLinksAsync(_articleId, html);

        // Other article's link should be untouched
        var otherLinks = await _context.ArticleExternalLinks
            .Where(l => l.ArticleId == otherArticleId)
            .ToListAsync();
        Assert.Single(otherLinks);
        Assert.Equal("Fireball", otherLinks[0].DisplayTitle);
    }

    [Fact]
    public async Task Sync_SkipsSpansWithMissingSource()
    {
        var html = @"<span data-type=""external-link"" data-source="""" data-id=""spells/fireball"" data-title=""Fireball"">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_SkipsSpansWithMissingExternalId()
    {
        var html = @"<span data-type=""external-link"" data-source=""srd14"" data-id="""" data-title=""Fireball"">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_SkipsSpansWithMissingTitle()
    {
        var html = @"<span data-type=""external-link"" data-source=""srd14"" data-id=""spells/fireball"" data-title="""">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Sync_SkipsSpansWithMissingAttributes()
    {
        var html = @"
            <span data-type=""external-link"" data-id=""spells/fireball"" data-title=""Fireball"">text</span>
            <span data-type=""external-link"" data-source=""srd14"" data-title=""Fireball"">text</span>
            <span data-type=""external-link"" data-source=""srd14"" data-id=""spells/fireball"">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var count = await _context.ArticleExternalLinks.CountAsync();
        Assert.Equal(0, count);
    }

    // ── GetExternalLinksForArticleAsync ───────────────────────────

    [Fact]
    public async Task Get_ReturnsEmptyList_WhenNoLinksExist()
    {
        var result = await _service.GetExternalLinksForArticleAsync(_articleId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Get_ReturnsOnlyLinksForRequestedArticle()
    {
        var otherArticleId = Guid.NewGuid();
        await SeedExternalLinks(_articleId, ("srd14", "spells/fireball", "Fireball"));
        await SeedExternalLinks(otherArticleId, ("open5e", "classes/wizard", "Wizard"));

        var result = await _service.GetExternalLinksForArticleAsync(_articleId);

        Assert.Single(result);
        Assert.Equal("Fireball", result[0].DisplayTitle);
    }

    [Fact]
    public async Task Get_ReturnsLinksOrderedBySourceThenTitle()
    {
        await SeedExternalLinks(_articleId,
            ("srd14", "spells/fireball", "Fireball"),
            ("open5e", "classes/wizard", "Wizard"),
            ("open5e", "classes/bard", "Bard"),
            ("srd14", "monsters/dragon", "Dragon"));

        var result = await _service.GetExternalLinksForArticleAsync(_articleId);

        Assert.Equal(4, result.Count);
        Assert.Equal("open5e", result[0].Source);
        Assert.Equal("Bard", result[0].DisplayTitle);
        Assert.Equal("open5e", result[1].Source);
        Assert.Equal("Wizard", result[1].DisplayTitle);
        Assert.Equal("srd14", result[2].Source);
        Assert.Equal("Dragon", result[2].DisplayTitle);
        Assert.Equal("srd14", result[3].Source);
        Assert.Equal("Fireball", result[3].DisplayTitle);
    }

    [Fact]
    public async Task Get_MapsAllDtoFieldsCorrectly()
    {
        await SeedExternalLinks(_articleId, ("srd14", "spells/fireball", "Fireball"));

        var result = await _service.GetExternalLinksForArticleAsync(_articleId);

        Assert.Single(result);
        var dto = result[0];
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(_articleId, dto.ArticleId);
        Assert.Equal("srd14", dto.Source);
        Assert.Equal("spells/fireball", dto.ExternalId);
        Assert.Equal("Fireball", dto.DisplayTitle);
    }

    // ── Attribute ordering robustness ────────────────────────────

    [Fact]
    public async Task Sync_ParsesAttributes_RegardlessOfOrder()
    {
        // title before source before id — opposite of the "standard" order
        var html = @"<span data-type=""external-link"" data-title=""Fireball"" data-source=""srd14"" data-id=""spells/fireball"">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var links = await _context.ArticleExternalLinks.ToListAsync();
        Assert.Single(links);
        Assert.Equal("srd14", links[0].Source);
        Assert.Equal("spells/fireball", links[0].ExternalId);
        Assert.Equal("Fireball", links[0].DisplayTitle);
    }

    [Fact]
    public async Task Sync_ParsesAttributes_WithDataTypeInMiddle()
    {
        // data-type appears between other data attributes
        var html = @"<span data-source=""open5e"" data-type=""external-link"" data-title=""Wizard"" data-id=""classes/wizard"">text</span>";

        await _service.SyncExternalLinksAsync(_articleId, html);

        var links = await _context.ArticleExternalLinks.ToListAsync();
        Assert.Single(links);
        Assert.Equal("open5e", links[0].Source);
        Assert.Equal("classes/wizard", links[0].ExternalId);
        Assert.Equal("Wizard", links[0].DisplayTitle);
    }
}
