using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
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
    private readonly IMapBlobStore _mapBlobStore;
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
        _mapBlobStore = Substitute.For<IMapBlobStore>();
        _mapBlobStore.GenerateReadSasUrlAsync(Arg.Any<string>())
            .Returns(args => Task.FromResult($"https://maps.test/{args.Arg<string>()}"));

        _sut = new PublicWorldService(
            _context,
            NullLogger<PublicWorldService>.Instance,
            _hierarchyService,
            _blobStorage,
            new ReadAccessPolicyService(),
            _mapBlobStore);
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
    public async Task GetPublicArticleTreeAsync_LegacySessionArticlePresent_UsesCanonicalVirtualSessionNode()
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

        Assert.True(sessionNode.IsVirtualGroup);
        Assert.Contains(sessionNode.Children!, c => c.Id == rootNote.Id);
    }

    [Fact]
    public async Task GetPublicArticleAsync_LegacySessionPrefixPath_IsRetired_AndCanonicalPathResolves()
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

        var legacyPathArticle = await _sut.GetPublicArticleAsync(
            seed.World.PublicSlug!,
            $"{seed.LegacySessionArticleSlug}/{rootNote.Slug}");
        var canonicalPathArticle = await _sut.GetPublicArticleAsync(
            seed.World.PublicSlug!,
            rootNote.Slug);

        Assert.Null(legacyPathArticle);
        Assert.NotNull(canonicalPathArticle);
        Assert.Equal(rootNote.Id, canonicalPathArticle!.Id);
    }

    [Fact]
    public async Task GetPublicArticlePathAsync_RootSessionNoteWithLegacySessionArticle_ReturnsCanonicalNoteSlug()
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

        Assert.Equal(rootNote.Slug, path);
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

    [Fact]
    public async Task GetPublicMapBasemapReadUrlAsync_ReturnsUrl_WhenMapBelongsToPublicWorld()
    {
        var seed = await SeedPublicWorldWithMapAsync();

        var result = await _sut.GetPublicMapBasemapReadUrlAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);

        Assert.NotNull(result.Basemap);
        Assert.Null(result.Error);
        Assert.Equal($"https://maps.test/{seed.Map.BasemapBlobKey}", result.Basemap!.ReadUrl);
        await _mapBlobStore.Received(1).GenerateReadSasUrlAsync(seed.Map.BasemapBlobKey!);
    }

    [Fact]
    public async Task GetPublicMapBasemapReadUrlAsync_ReturnsError_WhenBasemapMissing()
    {
        var seed = await SeedPublicWorldWithMapAsync();
        seed.Map.BasemapBlobKey = null;
        await _context.SaveChangesAsync();

        var result = await _sut.GetPublicMapBasemapReadUrlAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);

        Assert.Null(result.Basemap);
        Assert.Equal("Basemap is missing for this map.", result.Error);
        await _mapBlobStore.DidNotReceive().GenerateReadSasUrlAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task PublicMapQueries_ReturnProjectedData_AndHidePrivateLinkedArticles()
    {
        var seed = await SeedPublicWorldWithMapAsync();
        _mapBlobStore.LoadFeatureGeometryAsync(seed.PolygonFeature.GeometryBlobKey!)
            .Returns(Task.FromResult<string?>(
                "{\"type\":\"Polygon\",\"coordinates\":[[[0.1,0.1],[0.6,0.1],[0.3,0.5],[0.1,0.1]]]}"));

        var layers = await _sut.GetPublicMapLayersAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);
        var pins = await _sut.GetPublicMapPinsAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);
        var features = await _sut.GetPublicMapFeaturesAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);

        Assert.NotNull(layers);
        Assert.Equal(new[] { seed.RootLayer.MapLayerId, seed.RegionLayer.MapLayerId }, layers!.Select(layer => layer.MapLayerId));

        var pin = Assert.Single(pins!);
        Assert.Equal(seed.PointFeature.MapFeatureId, pin.PinId);
        Assert.Equal(seed.PublicArticle.Id, pin.LinkedArticle!.ArticleId);
        Assert.Equal(seed.PublicArticle.Title, pin.LinkedArticle.Title);

        Assert.Equal(2, features!.Count);
        var pointFeature = Assert.Single(features, feature => feature.FeatureId == seed.PointFeature.MapFeatureId);
        Assert.Equal(seed.PublicArticle.Title, pointFeature.LinkedArticle!.Title);

        var polygonFeature = Assert.Single(features, feature => feature.FeatureId == seed.PolygonFeature.MapFeatureId);
        Assert.Null(polygonFeature.LinkedArticle);
        Assert.NotNull(polygonFeature.Polygon);
        Assert.Equal(seed.PolygonFeature.GeometryBlobKey, polygonFeature.Geometry!.BlobKey);
    }

    [Fact]
    public async Task GetPublicMapLayersAsync_ReturnsNull_WhenMapIsNotInPublicWorld()
    {
        var seed = await SeedPublicWorldWithMapAsync();

        var result = await _sut.GetPublicMapLayersAsync(seed.World.PublicSlug!, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task PublicMapQueries_ReturnNull_WhenWorldSlugIsNotPublic()
    {
        var result = await _sut.GetPublicMapFeaturesAsync("missing-world", Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task PublicMapQueries_HandleMissingLinkedArticles()
    {
        var seed = await SeedPublicWorldWithMapAsync();
        seed.PointFeature.LinkedArticleId = null;
        seed.PolygonFeature.LinkedArticleId = null;
        await _context.SaveChangesAsync();
        _mapBlobStore.LoadFeatureGeometryAsync(seed.PolygonFeature.GeometryBlobKey!)
            .Returns(Task.FromResult<string?>(
                "{\"type\":\"Polygon\",\"coordinates\":[[[0.1,0.1],[0.6,0.1],[0.3,0.5],[0.1,0.1]]]}"));

        var pins = await _sut.GetPublicMapPinsAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);
        var features = await _sut.GetPublicMapFeaturesAsync(seed.World.PublicSlug!, seed.Map.WorldMapId);

        Assert.Null(Assert.Single(pins!).LinkedArticle);
        Assert.All(features!, feature => Assert.Null(feature.LinkedArticle));
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

    private async Task<(
        User Owner,
        World World,
        WorldMap Map,
        MapLayer RootLayer,
        MapLayer RegionLayer,
        Article PublicArticle,
        Article PrivateArticle,
        MapFeature PointFeature,
        MapFeature PolygonFeature)> SeedPublicWorldWithMapAsync()
    {
        var owner = TestHelpers.CreateUser();
        var world = TestHelpers.CreateWorld(ownerId: owner.Id, name: "Public World", slug: "internal-world");
        world.IsPublic = true;
        world.PublicSlug = "public-world";

        var publicArticle = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner.Id,
            title: "Visible Lore",
            slug: "visible-lore",
            visibility: ArticleVisibility.Public);

        var privateArticle = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner.Id,
            title: "Private Lore",
            slug: "private-lore",
            visibility: ArticleVisibility.Private);

        var map = new WorldMap
        {
            WorldMapId = Guid.NewGuid(),
            WorldId = world.Id,
            Name = "Roshar",
            BasemapBlobKey = "maps/roshar/basemap.png",
            BasemapContentType = "image/png",
            BasemapOriginalFilename = "roshar.png",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        var rootLayer = new MapLayer
        {
            MapLayerId = Guid.NewGuid(),
            WorldMapId = map.WorldMapId,
            Name = "World",
            SortOrder = 0,
            IsEnabled = true
        };

        var regionLayer = new MapLayer
        {
            MapLayerId = Guid.NewGuid(),
            WorldMapId = map.WorldMapId,
            ParentLayerId = rootLayer.MapLayerId,
            Name = "Regions",
            SortOrder = 1,
            IsEnabled = true
        };

        var pointFeature = new MapFeature
        {
            MapFeatureId = Guid.NewGuid(),
            WorldMapId = map.WorldMapId,
            MapLayerId = rootLayer.MapLayerId,
            FeatureType = MapFeatureType.Point,
            Name = "Shattered Plains",
            X = 0.25f,
            Y = 0.75f,
            LinkedArticleId = publicArticle.Id
        };

        var polygonFeature = new MapFeature
        {
            MapFeatureId = Guid.NewGuid(),
            WorldMapId = map.WorldMapId,
            MapLayerId = regionLayer.MapLayerId,
            FeatureType = MapFeatureType.Polygon,
            Name = "Alethkar",
            Color = "#C4AF8E",
            GeometryBlobKey = "maps/roshar/layers/regions/features/alethkar.geojson.gz",
            GeometryETag = "\"etag\"",
            LinkedArticleId = privateArticle.Id
        };

        _context.Users.Add(owner);
        _context.Worlds.Add(world);
        _context.Articles.AddRange(publicArticle, privateArticle);
        _context.WorldMaps.Add(map);
        _context.MapLayers.AddRange(rootLayer, regionLayer);
        _context.MapFeatures.AddRange(pointFeature, polygonFeature);
        await _context.SaveChangesAsync();

        return (owner, world, map, rootLayer, regionLayer, publicArticle, privateArticle, pointFeature, polygonFeature);
    }
}
