using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class LinkSyncServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly LinkSyncService _service;
    private readonly LinkParser _linkParser;
    private readonly Guid _articleId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public LinkSyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _linkParser = new LinkParser();
        _service = new LinkSyncService(_context, _linkParser, NullLogger<LinkSyncService>.Instance);
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

    // ────────────────────────────────────────────────────────────────
    //  SyncLinksAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncLinksAsync_NewLinks_CreatesLinks()
    {
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var body = $"Check out [[{targetId}|Reference Article]].";

        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Single(links);
        Assert.Equal(_articleId, links[0].SourceArticleId);
        Assert.Equal(targetId, links[0].TargetArticleId);
        Assert.Equal("Reference Article", links[0].DisplayText);
    }

    [Fact]
    public async Task SyncLinksAsync_MultipleLinks_CreatesAll()
    {
        var target1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var target2 = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var body = $"See [[{target1}|First]] and [[{target2}|Second]].";

        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public async Task SyncLinksAsync_NoLinks_CreatesNone()
    {
        var body = "Just plain text with no links.";

        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Empty(links);
    }

    [Fact]
    public async Task SyncLinksAsync_NullBody_CreatesNone()
    {
        await _service.SyncLinksAsync(_articleId, null);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Empty(links);
    }

    [Fact]
    public async Task SyncLinksAsync_ExistingLinks_RemovesOld()
    {
        var oldTarget = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var newTarget = Guid.Parse("20000000-0000-0000-0000-000000000002");

        // Create existing link
        var oldLink = new Chronicis.Shared.Models.ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = _articleId,
            TargetArticleId = oldTarget,
            CreatedAt = DateTime.UtcNow
        };
        _context.ArticleLinks.Add(oldLink);
        await _context.SaveChangesAsync();

        // Sync with new content
        var body = $"Now linking to [[{newTarget}]].";
        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Single(links);
        Assert.Equal(newTarget, links[0].TargetArticleId);
        Assert.DoesNotContain(links, l => l.TargetArticleId == oldTarget);
    }

    [Fact]
    public async Task SyncLinksAsync_UpdatedLinks_ReplacesAll()
    {
        var target1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var target2 = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var target3 = Guid.Parse("20000000-0000-0000-0000-000000000003");

        // Initial sync with 2 links
        var initialBody = $"[[{target1}]] and [[{target2}]]";
        await _service.SyncLinksAsync(_articleId, initialBody);

        var initialLinks = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Equal(2, initialLinks.Count);

        // Update to different links
        var updatedBody = $"Now only [[{target3}]]";
        await _service.SyncLinksAsync(_articleId, updatedBody);

        var updatedLinks = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Single(updatedLinks);
        Assert.Equal(target3, updatedLinks[0].TargetArticleId);
    }

    [Fact]
    public async Task SyncLinksAsync_RemoveAllLinks_DeletesAll()
    {
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");

        // Initial sync with links
        var body = $"[[{targetId}]]";
        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Single(links);

        // Sync with no links
        await _service.SyncLinksAsync(_articleId, "No links here");

        var remainingLinks = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Empty(remainingLinks);
    }

    [Fact]
    public async Task SyncLinksAsync_PreservesLinksFromOtherArticles()
    {
        var otherArticleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");

        // Create link from another article
        var otherLink = new Chronicis.Shared.Models.ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = otherArticleId,
            TargetArticleId = targetId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ArticleLinks.Add(otherLink);
        await _context.SaveChangesAsync();

        // Sync our article (should not affect other article's links)
        await _service.SyncLinksAsync(_articleId, "No links");

        var otherArticleLinks = await _context.ArticleLinks.Where(l => l.SourceArticleId == otherArticleId).ToListAsync();
        Assert.Single(otherArticleLinks); // Should still exist
    }

    [Fact]
    public async Task SyncLinksAsync_SetsCreatedAtTimestamp()
    {
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var before = DateTime.UtcNow;

        await _service.SyncLinksAsync(_articleId, $"[[{targetId}]]");

        var after = DateTime.UtcNow;
        var link = await _context.ArticleLinks.FirstAsync(l => l.SourceArticleId == _articleId);

        Assert.True(link.CreatedAt >= before);
        Assert.True(link.CreatedAt <= after);
    }

    [Fact]
    public async Task SyncLinksAsync_StoresPositionData()
    {
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var body = $"Text before [[{targetId}]] text after";

        await _service.SyncLinksAsync(_articleId, body);

        var link = await _context.ArticleLinks.FirstAsync(l => l.SourceArticleId == _articleId);
        Assert.Equal(12, link.Position); // Position of "[["
    }

    [Fact]
    public async Task SyncLinksAsync_HtmlFormat_CreatesLinks()
    {
        var targetId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var body = $"<p>Check <span data-target-id=\"{targetId}\">HTML Link</span> out.</p>";

        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Single(links);
        Assert.Equal(targetId, links[0].TargetArticleId);
        Assert.Equal("HTML Link", links[0].DisplayText);
    }

    [Fact]
    public async Task SyncLinksAsync_MixedFormats_CreatesAllLinks()
    {
        var target1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var target2 = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var body = $"Legacy [[{target1}]] and HTML <span data-target-id=\"{target2}\">Link</span>";

        await _service.SyncLinksAsync(_articleId, body);

        var links = await _context.ArticleLinks.Where(l => l.SourceArticleId == _articleId).ToListAsync();
        Assert.Equal(2, links.Count);
    }
}
