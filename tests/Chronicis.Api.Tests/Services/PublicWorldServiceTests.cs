using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public sealed class PublicWorldServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleHierarchyService _hierarchyService;
    private readonly IBlobStorageService _blobStorage;
    private readonly PublicWorldService _sut;
    private bool _disposed;

    public PublicWorldServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _hierarchyService = Substitute.For<IArticleHierarchyService>();
        _hierarchyService.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new List<BreadcrumbDto>()));
        _blobStorage = Substitute.For<IBlobStorageService>();
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns(args => Task.FromResult($"https://cdn.test/{args.Arg<string>()}"));

        _sut = new PublicWorldService(
            _context,
            NullLogger<PublicWorldService>.Instance,
            _hierarchyService,
            _blobStorage,
            new ReadAccessPolicyService());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
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

    [Fact]
    public async Task GetPublicArticleTreeAsync_NewSessionEntity_OrganizesSessionNotesUnderCampaigns()
    {
        var seed = await SeedWorldWithSessionAsync(includeLegacySessionArticle: false);

        var rootNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: seed.Campaign.Id,
            arcId: seed.Arc.Id,
            createdBy: seed.Owner.Id,
            title: "My Notes",
            slug: "my-notes",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootNote.SessionId = seed.Session.Id;
        rootNote.ParentId = null;
        _context.Articles.Add(rootNote);
        await _context.SaveChangesAsync();

        var tree = await _sut.GetPublicArticleTreeAsync(seed.World.PublicSlug!);

        var campaignsGroup = tree.Single(g => g.Slug == "campaigns");
        var campaignNode = Assert.Single(campaignsGroup.Children!);
        var arcNode = Assert.Single(campaignNode.Children!);
        var sessionNode = Assert.Single(arcNode.Children!);

        Assert.True(sessionNode.IsVirtualGroup);
        Assert.Equal(seed.Session.Id, sessionNode.Id);
        Assert.Contains(sessionNode.Children!, c => c.Id == rootNote.Id);

        var uncategorized = tree.FirstOrDefault(g => g.Slug == "uncategorized");
        Assert.True(uncategorized == null || uncategorized.Children!.All(c => c.Id != rootNote.Id));
    }

    [Fact]
    public async Task GetPublicArticleTreeAsync_LegacySessionArticle_AttachesRootSessionNotesBySessionId()
    {
        var seed = await SeedWorldWithSessionAsync(includeLegacySessionArticle: true);

        var rootNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: seed.Campaign.Id,
            arcId: seed.Arc.Id,
            createdBy: seed.Owner.Id,
            title: "Post Refactor Root Note",
            slug: "post-refactor-root-note",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootNote.SessionId = seed.Session.Id;
        rootNote.ParentId = null;
        _context.Articles.Add(rootNote);
        await _context.SaveChangesAsync();

        var tree = await _sut.GetPublicArticleTreeAsync(seed.World.PublicSlug!);

        var campaignsGroup = tree.Single(g => g.Slug == "campaigns");
        var campaignNode = Assert.Single(campaignsGroup.Children!);
        var arcNode = Assert.Single(campaignNode.Children!);
        var sessionNode = arcNode.Children!.Single(c => c.Id == seed.Session.Id);

        Assert.False(sessionNode.IsVirtualGroup);
        Assert.Contains(sessionNode.Children!, c => c.Id == rootNote.Id);
    }

    [Fact]
    public async Task GetPublicArticleAsync_AllowsLegacySessionPath_ToRootSessionNoteAttachedBySessionId()
    {
        var seed = await SeedWorldWithSessionAsync(includeLegacySessionArticle: true);

        var rootNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: seed.Campaign.Id,
            arcId: seed.Arc.Id,
            createdBy: seed.Owner.Id,
            title: "Root Session Note",
            slug: "root-session-note",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootNote.SessionId = seed.Session.Id;
        rootNote.ParentId = null;
        _context.Articles.Add(rootNote);
        await _context.SaveChangesAsync();

        var article = await _sut.GetPublicArticleAsync(
            seed.World.PublicSlug!,
            $"{seed.LegacySessionArticleSlug}/{rootNote.Slug}");

        Assert.NotNull(article);
        Assert.Equal(rootNote.Id, article!.Id);
    }

    [Fact]
    public async Task GetPublicArticlePathAsync_RootSessionNoteWithLegacySessionPrefix_ReturnsSessionSegment()
    {
        var seed = await SeedWorldWithSessionAsync(includeLegacySessionArticle: true);

        var rootNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: seed.Campaign.Id,
            arcId: seed.Arc.Id,
            createdBy: seed.Owner.Id,
            title: "Path Test Note",
            slug: "path-test-note",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootNote.SessionId = seed.Session.Id;
        rootNote.ParentId = null;
        _context.Articles.Add(rootNote);
        await _context.SaveChangesAsync();

        var path = await _sut.GetPublicArticlePathAsync(seed.World.PublicSlug!, rootNote.Id);

        Assert.Equal($"{seed.LegacySessionArticleSlug}/{rootNote.Slug}", path);
    }

    [Fact]
    public async Task GetPublicArticlePathAsync_RootSessionNoteWithoutLegacySessionArticle_ReturnsNoteSlug()
    {
        var seed = await SeedWorldWithSessionAsync(includeLegacySessionArticle: false);

        var rootNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: seed.Campaign.Id,
            arcId: seed.Arc.Id,
            createdBy: seed.Owner.Id,
            title: "Path Test Note 2",
            slug: "path-test-note-2",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootNote.SessionId = seed.Session.Id;
        rootNote.ParentId = null;
        _context.Articles.Add(rootNote);
        await _context.SaveChangesAsync();

        var path = await _sut.GetPublicArticlePathAsync(seed.World.PublicSlug!, rootNote.Id);

        Assert.Equal(rootNote.Slug, path);
    }

    [Fact]
    public async Task GetPublicDocumentDownloadUrlAsync_ReturnsUrl_WhenDocumentAttachedToPublicArticleInPublicWorld()
    {
        var owner = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(ownerId: owner.Id, name: "Public World", slug: "internal-world");
        world.IsPublic = true;
        world.PublicSlug = "public-world";

        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner.Id,
            title: "Public Article",
            slug: "public-article",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Public);

        var document = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            ArticleId = article.Id,
            FileName = "image.png",
            Title = "Image",
            ContentType = "image/png",
            FileSizeBytes = 128,
            UploadedById = owner.Id,
            UploadedAt = DateTime.UtcNow,
            BlobPath = "worlds/path/image.png"
        };

        _context.Users.Add(owner);
        _context.Worlds.Add(world);
        _context.Articles.Add(article);
        _context.WorldDocuments.Add(document);
        await _context.SaveChangesAsync();

        var url = await _sut.GetPublicDocumentDownloadUrlAsync(document.Id);

        Assert.Equal("https://cdn.test/worlds/path/image.png", url);
        await _blobStorage.Received(1).GenerateDownloadSasUrlAsync("worlds/path/image.png");
    }

    [Fact]
    public async Task GetPublicDocumentDownloadUrlAsync_ReturnsNull_WhenArticleNotPublic()
    {
        var owner = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(ownerId: owner.Id, name: "Public World", slug: "internal-world");
        world.IsPublic = true;
        world.PublicSlug = "public-world";

        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner.Id,
            title: "Private Article",
            slug: "private-article",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Private);

        var document = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            ArticleId = article.Id,
            FileName = "image.png",
            Title = "Image",
            ContentType = "image/png",
            FileSizeBytes = 128,
            UploadedById = owner.Id,
            UploadedAt = DateTime.UtcNow,
            BlobPath = "worlds/path/private-image.png"
        };

        _context.Users.Add(owner);
        _context.Worlds.Add(world);
        _context.Articles.Add(article);
        _context.WorldDocuments.Add(document);
        await _context.SaveChangesAsync();

        var url = await _sut.GetPublicDocumentDownloadUrlAsync(document.Id);

        Assert.Null(url);
        await _blobStorage.DidNotReceive().GenerateDownloadSasUrlAsync(Arg.Any<string>());
    }

    private async Task<(User Owner, World World, Campaign Campaign, Arc Arc, Session Session, string LegacySessionArticleSlug)> SeedWorldWithSessionAsync(bool includeLegacySessionArticle)
    {
        var owner = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(
            ownerId: owner.Id,
            name: "Public World",
            slug: "internal-world");
        world.IsPublic = true;
        world.PublicSlug = "public-world";

        var campaign = TestHelpers.CreateCampaign(worldId: world.Id, name: "Campaign");
        campaign.OwnerId = owner.Id;
        campaign.CreatedAt = DateTime.UtcNow;
        campaign.StartedAt = DateTime.UtcNow.Date;
        campaign.IsActive = true;

        var arc = TestHelpers.CreateArc(campaignId: campaign.Id, name: "Arc", sortOrder: 0);
        arc.CreatedBy = owner.Id;
        arc.CreatedAt = DateTime.UtcNow;

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Session 1",
            SessionDate = DateTime.UtcNow.Date,
            CreatedBy = owner.Id,
            CreatedAt = DateTime.UtcNow,
            PublicNotes = "<p>Public notes</p>"
        };

        _context.Users.Add(owner);
        _context.Worlds.Add(world);
        _context.Campaigns.Add(campaign);
        _context.Arcs.Add(arc);
        _context.Sessions.Add(session);

        const string legacySessionSlug = "session-1";
        if (includeLegacySessionArticle)
        {
            var legacySessionArticle = TestHelpers.CreateArticle(
                id: session.Id,
                worldId: world.Id,
                campaignId: campaign.Id,
                arcId: arc.Id,
                createdBy: owner.Id,
                title: session.Name,
                slug: legacySessionSlug,
                type: ArticleType.Session,
                visibility: ArticleVisibility.Public);
            legacySessionArticle.ParentId = null;

            _context.Articles.Add(legacySessionArticle);
        }

        await _context.SaveChangesAsync();
        return (owner, world, campaign, arc, session, legacySessionSlug);
    }
}
