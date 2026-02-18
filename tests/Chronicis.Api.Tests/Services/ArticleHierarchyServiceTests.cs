using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;


[ExcludeFromCodeCoverage]
public class ArticleHierarchyServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleHierarchyService _service;

    // Fixed IDs for test data
    private static readonly Guid WorldId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid RootArticleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ChildArticleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid GrandchildArticleId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid PrivateArticleId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid CharacterArticleId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid SessionArticleId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid CampaignId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ArcId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    public ArticleHierarchyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new ArticleHierarchyService(
            _context,
            NullLogger<ArticleHierarchyService>.Instance);

        SeedTestData();
    }

    private bool _disposed = false;
    public void Dispose()
    {

        Dispose(true);
        // Suppress finalization, as cleanup has been done.
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
        var user = new User
        {
            Id = UserId,
            Auth0UserId = "auth0|test",
            Email = "test@test.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(user);

        var world = new World
        {
            Id = WorldId,
            Name = "Test World",
            Slug = "test-world",
            OwnerId = UserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Worlds.Add(world);

        var campaign = new Campaign
        {
            Id = CampaignId,
            Name = "Test Campaign",
            WorldId = WorldId,
            IsActive = true
        };
        _context.Campaigns.Add(campaign);

        var arc = new Arc
        {
            Id = ArcId,
            Name = "Test Arc",
            CampaignId = CampaignId,
            SortOrder = 1
        };
        _context.Arcs.Add(arc);

        // Root > Child > Grandchild
        _context.Articles.AddRange(
            new Article
            {
                Id = RootArticleId,
                Title = "Root Article",
                Slug = "root-article",
                ParentId = null,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            new Article
            {
                Id = ChildArticleId,
                Title = "Child Article",
                Slug = "child-article",
                ParentId = RootArticleId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            new Article
            {
                Id = GrandchildArticleId,
                Title = "Grandchild Article",
                Slug = "grandchild-article",
                ParentId = ChildArticleId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            // Private article as child of root
            new Article
            {
                Id = PrivateArticleId,
                Title = "Private Article",
                Slug = "private-article",
                ParentId = RootArticleId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Private,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            // Character article at root
            new Article
            {
                Id = CharacterArticleId,
                Title = "Aragorn",
                Slug = "aragorn",
                ParentId = null,
                WorldId = WorldId,
                Type = ArticleType.Character,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            // Session article with campaign/arc
            new Article
            {
                Id = SessionArticleId,
                Title = "Session 1",
                Slug = "session-1",
                ParentId = null,
                WorldId = WorldId,
                CampaignId = CampaignId,
                ArcId = ArcId,
                Type = ArticleType.Session,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            }
        );

        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  Basic breadcrumb building
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_RootArticle_ReturnsWorldAndArticle()
    {
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(RootArticleId);

        Assert.Equal(2, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Test World", breadcrumbs[0].Title);
        Assert.Equal("Root Article", breadcrumbs[1].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_GrandchildArticle_ReturnsFullChain()
    {
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(GrandchildArticleId);

        Assert.Equal(4, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Root Article", breadcrumbs[1].Title);
        Assert.Equal("Child Article", breadcrumbs[2].Title);
        Assert.Equal("Grandchild Article", breadcrumbs[3].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_WithoutWorldBreadcrumb_OmitsWorld()
    {
        var options = new HierarchyWalkOptions { IncludeWorldBreadcrumb = false };
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(GrandchildArticleId, options);

        Assert.Equal(3, breadcrumbs.Count);
        Assert.False(breadcrumbs[0].IsWorld);
        Assert.Equal("Root Article", breadcrumbs[0].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_ExcludeCurrentArticle_OmitsTarget()
    {
        var options = new HierarchyWalkOptions { IncludeCurrentArticle = false };
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(GrandchildArticleId, options);

        // World + Root + Child (grandchild excluded)
        Assert.Equal(3, breadcrumbs.Count);
        Assert.Equal("Child Article", breadcrumbs[^1].Title);
    }

    // ────────────────────────────────────────────────────────────────
    //  Public-only scoping
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_PublicOnly_IncludesPublicArticles()
    {
        var options = new HierarchyWalkOptions { PublicOnly = true };
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(GrandchildArticleId, options);

        // All articles in the chain are public, so full chain should be present
        Assert.Equal(4, breadcrumbs.Count);
    }

    [Fact]
    public async Task BuildBreadcrumbs_PublicOnly_StopsAtPrivateArticle()
    {
        // Create a chain: Root (public) > PrivateMiddle (private) > PublicLeaf (public)
        var privateMiddleId = Guid.Parse("10000000-0000-0000-0000-000000000010");
        var publicLeafId = Guid.Parse("10000000-0000-0000-0000-000000000011");

        _context.Articles.AddRange(
            new Article
            {
                Id = privateMiddleId,
                Title = "Private Middle",
                Slug = "private-middle",
                ParentId = RootArticleId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Private,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            new Article
            {
                Id = publicLeafId,
                Title = "Public Leaf",
                Slug = "public-leaf",
                ParentId = privateMiddleId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var options = new HierarchyWalkOptions { PublicOnly = true };
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(publicLeafId, options);

        // Walk: PublicLeaf -> PrivateMiddle (not found, walk stops)
        // So we only get World + PublicLeaf
        Assert.Equal(2, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Public Leaf", breadcrumbs[1].Title);
    }

    // ────────────────────────────────────────────────────────────────
    //  Cycle detection
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_CycleInHierarchy_DoesNotInfiniteLoop()
    {
        // Create a cycle: A -> B -> A
        var cycleAId = Guid.Parse("10000000-0000-0000-0000-0000000000A0");
        var cycleBId = Guid.Parse("10000000-0000-0000-0000-0000000000B0");

        _context.Articles.AddRange(
            new Article
            {
                Id = cycleAId,
                Title = "Cycle A",
                Slug = "cycle-a",
                ParentId = cycleBId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            },
            new Article
            {
                Id = cycleBId,
                Title = "Cycle B",
                Slug = "cycle-b",
                ParentId = cycleAId,
                WorldId = WorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(cycleAId);

        // Should terminate without throwing. Exact count depends on which node is hit first in the cycle.
        Assert.NotNull(breadcrumbs);
        // Should contain at most the world + the two articles (cycle broken after detecting revisit)
        Assert.True(breadcrumbs.Count <= 3);
    }

    // ────────────────────────────────────────────────────────────────
    //  Missing parents
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_MissingParent_StopsGracefully()
    {
        // Article with a non-existent parent
        var orphanId = Guid.Parse("10000000-0000-0000-0000-0000000000FF");
        _context.Articles.Add(new Article
        {
            Id = orphanId,
            Title = "Orphan",
            Slug = "orphan",
            ParentId = Guid.Parse("99999999-9999-9999-9999-999999999999"), // doesn't exist
            WorldId = WorldId,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedBy = UserId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(orphanId);

        // World + Orphan (parent walk stops when parent not found)
        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Orphan", breadcrumbs[^1].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_NonExistentArticle_ReturnsEmpty()
    {
        var breadcrumbs = await _service.BuildBreadcrumbsAsync(Guid.NewGuid());

        // No article found -> no breadcrumbs at all (world can't be resolved either)
        Assert.Empty(breadcrumbs);
    }

    // ────────────────────────────────────────────────────────────────
    //  Virtual groups
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_WithVirtualGroups_SessionArticle_IncludesCampaignAndArc()
    {
        var options = new HierarchyWalkOptions
        {
            IncludeVirtualGroups = true,
            PublicOnly = true,
            World = new WorldContext { Id = WorldId, Name = "Test World", Slug = "test-world" }
        };

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(SessionArticleId, options);

        // World + Campaign + Arc + Session article
        Assert.Equal(4, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Test Campaign", breadcrumbs[1].Title);
        Assert.Equal("Test Arc", breadcrumbs[2].Title);
        Assert.Equal("Session 1", breadcrumbs[3].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_WithVirtualGroups_CharacterArticle_IncludesPlayerCharactersGroup()
    {
        var options = new HierarchyWalkOptions
        {
            IncludeVirtualGroups = true,
            PublicOnly = true,
            World = new WorldContext { Id = WorldId, Name = "Test World", Slug = "test-world" }
        };

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(CharacterArticleId, options);

        // World + "Player Characters" virtual group + Character article
        Assert.Equal(3, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Player Characters", breadcrumbs[1].Title);
        Assert.Equal("Aragorn", breadcrumbs[2].Title);
    }

    [Fact]
    public async Task BuildBreadcrumbs_WithVirtualGroups_WikiArticle_IncludesWikiGroup()
    {
        var options = new HierarchyWalkOptions
        {
            IncludeVirtualGroups = true,
            PublicOnly = true,
            World = new WorldContext { Id = WorldId, Name = "Test World", Slug = "test-world" }
        };

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(RootArticleId, options);

        // World + "Wiki" virtual group + Root Article
        Assert.Equal(3, breadcrumbs.Count);
        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Wiki", breadcrumbs[1].Title);
        Assert.Equal("Root Article", breadcrumbs[2].Title);
    }

    // ────────────────────────────────────────────────────────────────
    //  Path building
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildPath_ReturnsSlashSeparatedSlugs()
    {
        var path = await _service.BuildPathAsync(GrandchildArticleId);

        Assert.Equal("test-world/root-article/child-article/grandchild-article", path);
    }

    // ────────────────────────────────────────────────────────────────
    //  Display path building
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildDisplayPath_StripsFirstLevel()
    {
        var path = await _service.BuildDisplayPathAsync(GrandchildArticleId, stripFirstLevel: true);

        Assert.Equal("Child Article / Grandchild Article", path);
    }

    [Fact]
    public async Task BuildDisplayPath_KeepsFirstLevel_WhenNotStripping()
    {
        var path = await _service.BuildDisplayPathAsync(GrandchildArticleId, stripFirstLevel: false);

        Assert.Equal("Root Article / Child Article / Grandchild Article", path);
    }

    [Fact]
    public async Task BuildDisplayPath_SingleArticle_NothingToStrip()
    {
        var path = await _service.BuildDisplayPathAsync(RootArticleId, stripFirstLevel: true);

        // Only one level, nothing to strip
        Assert.Equal("Root Article", path);
    }

    // ────────────────────────────────────────────────────────────────
    //  Pre-resolved WorldContext
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildBreadcrumbs_WithPreResolvedWorld_UsesProvidedValues()
    {
        var options = new HierarchyWalkOptions
        {
            World = new WorldContext
            {
                Id = WorldId,
                Name = "Custom World Name",
                Slug = "custom-slug"
            }
        };

        var breadcrumbs = await _service.BuildBreadcrumbsAsync(RootArticleId, options);

        Assert.True(breadcrumbs[0].IsWorld);
        Assert.Equal("Custom World Name", breadcrumbs[0].Title);
        Assert.Equal("custom-slug", breadcrumbs[0].Slug);
    }
}
