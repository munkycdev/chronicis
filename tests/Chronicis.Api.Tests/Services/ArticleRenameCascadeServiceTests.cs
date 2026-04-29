using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticleRenameCascadeServiceTests : IDisposable
{
    private readonly ChronicisDbContext _db;
    private readonly IWikiLinkTitleRewriter _rewriter;
    private readonly ILogger<ArticleRenameCascadeService> _logger;
    private bool _disposed;

    public ArticleRenameCascadeServiceTests()
    {
        var opts = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ChronicisDbContext(opts);
        _rewriter = Substitute.For<IWikiLinkTitleRewriter>();
        _logger = NullLogger<ArticleRenameCascadeService>.Instance;
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
            _db.Dispose();
        _disposed = true;
    }

    private ArticleRenameCascadeService Sut() =>
        new(_db, _rewriter, _logger);

    private static Article MakeArticle(Guid id, string body = "") =>
        new()
        {
            Id = id,
            Title = "Title",
            Slug = id.ToString("N")[..8],
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.MembersOnly,
            Body = body,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow.AddDays(-1),
            LastModifiedBy = Guid.NewGuid(),
        };

    private static ArticleLink MakeLink(Guid sourceId, Guid targetId, string? displayText = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SourceArticleId = sourceId,
            TargetArticleId = targetId,
            DisplayText = displayText,
            CreatedAt = DateTime.UtcNow,
        };

    // ── no-op / guard paths ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Dragon Keep", "Dragon Keep")]   // identical
    [InlineData("Dragon Keep", "dragon keep")]   // case-only difference
    [InlineData("Dragon Keep", "DRAGON KEEP")]   // all-caps
    public async Task NoOp_WhenOldAndNewTitleSameIgnoreCase(string oldTitle, string newTitle)
    {
        var renamedId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        _db.Articles.Add(MakeArticle(sourceId, "<p>body</p>"));
        _db.ArticleLinks.Add(MakeLink(sourceId, renamedId));
        await _db.SaveChangesAsync();

        await Sut().CascadeTitleChangeAsync(renamedId, oldTitle, newTitle);

        _rewriter.DidNotReceiveWithAnyArgs().Rewrite(default, default, default!);
        // No alias should have been appended
        Assert.Empty(await _db.ArticleAliases.Where(a => a.ArticleId == renamedId).ToListAsync());
    }

    [Fact]
    public async Task Throws_WhenNewTitleWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Sut().CascadeTitleChangeAsync(Guid.NewGuid(), "Old", "   "));
    }
    // ── backlink filtering ────────────────────────────────────────────────────

    [Fact]
    public async Task QueriesOnlyBacklinksWithNullDisplayText()
    {
        var renamedId = Guid.NewGuid();
        var sourceA = Guid.NewGuid();
        var sourceB = Guid.NewGuid();
        var body = "<p>some body</p>";

        _db.Articles.AddRange(MakeArticle(sourceA, body), MakeArticle(sourceB, body));
        _db.ArticleLinks.AddRange(
            MakeLink(sourceA, renamedId, null),           // eligible
            MakeLink(sourceB, renamedId, "custom label")); // user override — skip
        await _db.SaveChangesAsync();

        _rewriter.Rewrite(body, renamedId, "New")
                 .Returns((body, false));

        await Sut().CascadeTitleChangeAsync(renamedId, "Old", "New");

        _rewriter.Received(1).Rewrite(Arg.Any<string>(), renamedId, "New");
    }

    [Fact]
    public async Task IgnoresCustomDisplayTextBacklinks()
    {
        var renamedId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        _db.Articles.Add(MakeArticle(sourceId, "<p>body</p>"));
        _db.ArticleLinks.Add(MakeLink(sourceId, renamedId, "my custom label"));
        await _db.SaveChangesAsync();

        await Sut().CascadeTitleChangeAsync(renamedId, "Old", "New");

        _rewriter.DidNotReceiveWithAnyArgs().Rewrite(default, default, default!);
    }

    // ── body write behaviour ──────────────────────────────────────────────────

    [Fact]
    public async Task WritesRewrittenBody_OnlyWhenChanged()
    {
        var renamedId = Guid.NewGuid();
        var sourceA = Guid.NewGuid();
        var sourceB = Guid.NewGuid();
        var bodyA = "<p>unchanged</p>";
        var bodyB = "<p>will change</p>";
        var newBodyB = "<p>changed</p>";

        var articleA = MakeArticle(sourceA, bodyA);
        var articleB = MakeArticle(sourceB, bodyB);
        _db.Articles.AddRange(articleA, articleB);
        _db.ArticleLinks.AddRange(
            MakeLink(sourceA, renamedId),
            MakeLink(sourceB, renamedId));
        await _db.SaveChangesAsync();

        _rewriter.Rewrite(bodyA, renamedId, "New").Returns((bodyA, false));
        _rewriter.Rewrite(bodyB, renamedId, "New").Returns((newBodyB, true));

        await Sut().CascadeTitleChangeAsync(renamedId, "Old", "New");

        var savedA = await _db.Articles.FindAsync(sourceA);
        var savedB = await _db.Articles.FindAsync(sourceB);
        Assert.Equal(bodyA, savedA!.Body);
        Assert.Equal(newBodyB, savedB!.Body);
    }

    [Fact]
    public async Task DoesNotModifyModifiedAtOrLastModifiedBy_OnCascadedSources()
    {
        var renamedId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var staleDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var staleUser = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
        var body = "<p>body</p>";
        var newBody = "<p>new body</p>";

        var article = MakeArticle(sourceId, body);
        article.ModifiedAt = staleDate;
        article.LastModifiedBy = staleUser;
        _db.Articles.Add(article);
        _db.ArticleLinks.Add(MakeLink(sourceId, renamedId));
        await _db.SaveChangesAsync();

        _rewriter.Rewrite(body, renamedId, "New").Returns((newBody, true));

        await Sut().CascadeTitleChangeAsync(renamedId, "Old", "New");

        var saved = await _db.Articles.FindAsync(sourceId);
        Assert.Equal(staleDate, saved!.ModifiedAt);
        Assert.Equal(staleUser, saved.LastModifiedBy);
    }
    // ── alias management ──────────────────────────────────────────────────────

    [Fact]
    public async Task AppendsAliasForOldTitle_OnRenamedArticle()
    {
        var renamedId = Guid.NewGuid();
        _db.Articles.Add(MakeArticle(renamedId));
        await _db.SaveChangesAsync();

        await Sut().CascadeTitleChangeAsync(renamedId, "Old Title", "New Title");

        var aliases = await _db.ArticleAliases
            .Where(a => a.ArticleId == renamedId)
            .ToListAsync();
        Assert.Single(aliases);
        Assert.Equal("Old Title", aliases[0].AliasText);
    }

    [Fact]
    public async Task AppendAlias_WhenZeroIncomingLinks()
    {
        // Alias must still be added even when no backlinks exist.
        var renamedId = Guid.NewGuid();
        _db.Articles.Add(MakeArticle(renamedId));
        await _db.SaveChangesAsync();

        await Sut().CascadeTitleChangeAsync(renamedId, "Old Title", "New Title");

        var aliases = await _db.ArticleAliases
            .Where(a => a.ArticleId == renamedId)
            .ToListAsync();
        Assert.Single(aliases);
    }

    [Fact]
    public async Task DoesNotDuplicateAlias_WhenAlreadyPresent()
    {
        var renamedId = Guid.NewGuid();
        _db.Articles.Add(MakeArticle(renamedId));
        _db.ArticleAliases.Add(new ArticleAlias
        {
            Id = Guid.NewGuid(),
            ArticleId = renamedId,
            AliasText = "old title",   // lowercase — case-insensitive dedup
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        await Sut().CascadeTitleChangeAsync(renamedId, "Old Title", "New Title");

        var count = await _db.ArticleAliases.CountAsync(a => a.ArticleId == renamedId);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SkipsAliasAppend_WhenOldTitleWhitespaceOrEmpty(string oldTitle)
    {
        var renamedId = Guid.NewGuid();
        await Sut().CascadeTitleChangeAsync(renamedId, oldTitle, "New Title");

        Assert.Empty(await _db.ArticleAliases.Where(a => a.ArticleId == renamedId).ToListAsync());
    }

    // ── persistence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PersistsBodyAndAlias_InSingleOperation()
    {
        // Verifies body rewrite AND alias insert are both committed together.
        var renamedId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var body = "<p>body</p>";
        var newBody = "<p>new body</p>";

        _db.Articles.AddRange(MakeArticle(renamedId), MakeArticle(sourceId, body));
        _db.ArticleLinks.Add(MakeLink(sourceId, renamedId));
        await _db.SaveChangesAsync();

        _rewriter.Rewrite(body, renamedId, "New").Returns((newBody, true));

        await Sut().CascadeTitleChangeAsync(renamedId, "Old", "New");

        // Both changes persisted
        var savedBody = (await _db.Articles.FindAsync(sourceId))!.Body;
        var aliasCount = await _db.ArticleAliases.CountAsync(a => a.ArticleId == renamedId);
        Assert.Equal(newBody, savedBody);
        Assert.Equal(1, aliasCount);
    }
}
