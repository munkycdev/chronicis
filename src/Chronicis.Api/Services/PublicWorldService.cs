using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for anonymous public access to worlds.
/// All methods return only publicly visible content - no authentication required.
/// </summary>
public sealed class PublicWorldService : IPublicWorldService
{
    private static readonly JsonSerializerOptions GeometryJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ChronicisDbContext _context;
    private readonly ILogger<PublicWorldService> _logger;
    private readonly IArticleHierarchyService _hierarchyService;
    private readonly IBlobStorageService _blobStorage;
    private readonly IReadAccessPolicyService _readAccessPolicy;
    private readonly IMapBlobStore _mapBlobStore;

    public PublicWorldService(
        ChronicisDbContext context,
        ILogger<PublicWorldService> logger,
        IArticleHierarchyService hierarchyService,
        IBlobStorageService blobStorage,
        IReadAccessPolicyService readAccessPolicy,
        IMapBlobStore mapBlobStore)
    {
        _context = context;
        _logger = logger;
        _hierarchyService = hierarchyService;
        _blobStorage = blobStorage;
        _readAccessPolicy = readAccessPolicy;
        _mapBlobStore = mapBlobStore;
    }

    /// <summary>
    /// Get a public world by its public slug.
    /// Returns null if world doesn't exist or is not public.
    /// </summary>
    public async Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug)
    {
        var normalizedSlug = _readAccessPolicy.NormalizePublicSlug(publicSlug);

        var world = await _readAccessPolicy
            .ApplyPublicWorldSlugFilter(_context.Worlds.AsNoTracking(), normalizedSlug)
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogTraceSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        _logger.LogTraceSanitized("Public world '{WorldName}' accessed via slug '{PublicSlug}'",
            world.Name, normalizedSlug);

        return new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            IsPublic = world.IsPublic,
            IsTutorial = world.IsTutorial,
            PublicSlug = world.PublicSlug,
            // Include public campaigns
            Campaigns = world.Campaigns?.Select(c => new CampaignDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                WorldId = c.WorldId,
                IsActive = c.IsActive,
                StartedAt = c.StartedAt
            }).ToList() ?? new List<CampaignDto>()
        };
    }

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// Returns a hierarchical tree structure organized by virtual groups (Campaigns, Characters, Wiki).
    /// </summary>
    public async Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug)
    {
        var normalizedSlug = _readAccessPolicy.NormalizePublicSlug(publicSlug);

        // First, verify the world exists and is public
        var world = await _readAccessPolicy
            .ApplyPublicWorldSlugFilter(_context.Worlds.AsNoTracking(), normalizedSlug)
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
                    .ThenInclude(a => a.SessionEntities)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogTraceSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return new List<ArticleTreeDto>();
        }

        // Get all public articles for this world
        var allPublicArticles = await _readAccessPolicy
            .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), world.Id)
            .Select(a => new ArticleTreeDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                SessionId = a.SessionId,
                Type = a.Type,
                Visibility = a.Visibility,
                HasChildren = false, // Will calculate below
                ChildCount = 0,      // Will calculate below
                Children = new List<ArticleTreeDto>(),
                CreatedAt = a.CreatedAt,
                EffectiveDate = a.EffectiveDate,
                IconEmoji = a.IconEmoji,
                CreatedBy = a.CreatedBy
            })
            .ToListAsync();

        // Build article index and children relationships
        var articleIndex = allPublicArticles.ToDictionary(a => a.Id);
        var sessionNotesBySessionId = allPublicArticles
            .Where(a => a.Type == ArticleType.SessionNote && a.SessionId.HasValue)
            .GroupBy(a => a.SessionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Link children to parents
        foreach (var article in allPublicArticles)
        {
            if (article.ParentId.HasValue && articleIndex.TryGetValue(article.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<ArticleTreeDto>();
                parent.Children.Add(article);
                parent.HasChildren = true;
                parent.ChildCount++;
            }
        }

        // Sort children by title
        foreach (var article in allPublicArticles.Where(a => a.Children?.Any() == true))
        {
            article.Children = article.Children!.OrderBy(c => c.Title).ToList();
        }

        // Build virtual groups
        var result = new List<ArticleTreeDto>();

        // 1. Campaigns group - contains campaigns with their arcs and sessions
        var campaignsGroup = CreateVirtualGroup("campaigns", "Campaigns", "fa-solid fa-dungeon");
        foreach (var campaign in world.Campaigns?.OrderBy(c => c.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Campaign>())
        {
            var campaignNode = new ArticleTreeDto
            {
                Id = campaign.Id,
                Title = campaign.Name,
                Slug = campaign.Name.ToLowerInvariant().Replace(" ", "-"),
                Type = ArticleType.WikiArticle, // Use WikiArticle as placeholder
                IconEmoji = "fa-solid fa-dungeon",
                Children = new List<ArticleTreeDto>(),
                IsVirtualGroup = true
            };

            // Add arcs under campaign
            foreach (var arc in campaign.Arcs?.OrderBy(a => a.SortOrder).ThenBy(a => a.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Arc>())
            {
                var arcNode = new ArticleTreeDto
                {
                    Id = arc.Id,
                    Title = arc.Name,
                    Slug = arc.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = ArticleType.WikiArticle,
                    IconEmoji = "fa-solid fa-book-open",
                    Children = new List<ArticleTreeDto>(),
                    IsVirtualGroup = true
                };

                var sessions = arc.SessionEntities?
                    .OrderBy(s => s.SessionDate ?? DateTime.MaxValue)
                    .ThenBy(s => s.Name)
                    .ThenBy(s => s.CreatedAt)
                    ?? Enumerable.Empty<Chronicis.Shared.Models.Session>();

                foreach (var session in sessions)
                {
                    var rootSessionNotes = GetRootSessionNotesForSession(sessionNotesBySessionId, session.Id);

                    if (!rootSessionNotes.Any())
                    {
                        continue;
                    }

                    var sessionNode = new ArticleTreeDto
                    {
                        Id = session.Id,
                        Title = session.Name,
                        Slug = session.Name.ToLowerInvariant().Replace(" ", "-"),
                        Type = ArticleType.Session,
                        IconEmoji = "fa-solid fa-calendar-day",
                        Children = new List<ArticleTreeDto>(),
                        IsVirtualGroup = true,
                        HasAISummary = !string.IsNullOrWhiteSpace(session.AiSummary)
                    };

                    foreach (var note in rootSessionNotes)
                    {
                        sessionNode.Children.Add(note);
                        sessionNode.HasChildren = true;
                        sessionNode.ChildCount++;
                    }

                    arcNode.Children.Add(sessionNode);
                    arcNode.HasChildren = true;
                    arcNode.ChildCount++;
                }

                if (arcNode.Children.Any())
                {
                    campaignNode.Children.Add(arcNode);
                    campaignNode.HasChildren = true;
                    campaignNode.ChildCount++;
                }
            }

            if (campaignNode.Children.Any())
            {
                campaignsGroup.Children!.Add(campaignNode);
                campaignsGroup.HasChildren = true;
                campaignsGroup.ChildCount++;
            }
        }

        if (campaignsGroup.Children!.Any())
        {
            result.Add(campaignsGroup);
        }

        // 2. Player Characters group
        var charactersGroup = CreateVirtualGroup("characters", "Player Characters", "fa-solid fa-user-ninja");
        var characterArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.Character && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in characterArticles)
        {
            charactersGroup.Children!.Add(article);
            charactersGroup.HasChildren = true;
            charactersGroup.ChildCount++;
        }

        if (charactersGroup.Children!.Any())
        {
            result.Add(charactersGroup);
        }

        // 3. Wiki group
        var wikiGroup = CreateVirtualGroup("wiki", "Wiki", "fa-solid fa-book");
        var wikiArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.WikiArticle && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in wikiArticles)
        {
            wikiGroup.Children!.Add(article);
            wikiGroup.HasChildren = true;
            wikiGroup.ChildCount++;
        }

        if (wikiGroup.Children!.Any())
        {
            result.Add(wikiGroup);
        }

        // 4. Uncategorized (Legacy and other types without parents not already included)
        var includedIds = new HashSet<Guid>();

        // Collect all IDs from campaigns/arcs/sessions
        foreach (var campaign in result.FirstOrDefault(r => r.Slug == "campaigns")?.Children ?? new List<ArticleTreeDto>())
        {
            foreach (var arc in campaign.Children ?? new List<ArticleTreeDto>())
            {
                foreach (var session in arc.Children ?? new List<ArticleTreeDto>())
                {
                    CollectArticleIds(session, includedIds);
                }
            }
        }

        // Collect character and wiki IDs
        foreach (var article in characterArticles)
        {
            CollectArticleIds(article, includedIds);
        }
        foreach (var article in wikiArticles)
        {
            CollectArticleIds(article, includedIds);
        }

        var uncategorizedArticles = allPublicArticles
            .Where(a => a.ParentId == null &&
                        !includedIds.Contains(a.Id))
            .OrderBy(a => a.Title)
            .ToList();

        if (uncategorizedArticles.Any())
        {
            var uncategorizedGroup = CreateVirtualGroup("uncategorized", "Uncategorized", "fa-solid fa-folder");
            foreach (var article in uncategorizedArticles)
            {
                uncategorizedGroup.Children!.Add(article);
                uncategorizedGroup.HasChildren = true;
                uncategorizedGroup.ChildCount++;
            }
            result.Add(uncategorizedGroup);
        }

        _logger.LogTraceSanitized("Retrieved {Count} public articles for world '{PublicSlug}' in {GroupCount} groups",
            allPublicArticles.Count, normalizedSlug, result.Count);

        return result;
    }

    private static List<ArticleTreeDto> GetRootSessionNotesForSession(
        IReadOnlyDictionary<Guid, List<ArticleTreeDto>> sessionNotesBySessionId,
        Guid sessionId)
    {
        if (!sessionNotesBySessionId.TryGetValue(sessionId, out var notes))
        {
            return new List<ArticleTreeDto>();
        }

        var sessionNoteIds = notes.Select(n => n.Id).ToHashSet();
        return notes
            .Where(n => !n.ParentId.HasValue || !sessionNoteIds.Contains(n.ParentId.Value))
            .OrderBy(n => n.Title)
            .ToList();
    }

    private static ArticleTreeDto CreateVirtualGroup(string slug, string title, string icon)
    {
        return new ArticleTreeDto
        {
            Id = Guid.NewGuid(), // Virtual ID
            Title = title,
            Slug = slug,
            Type = ArticleType.WikiArticle,
            IconEmoji = icon,
            HasChildren = false,
            ChildCount = 0,
            Children = new List<ArticleTreeDto>(),
            IsVirtualGroup = true
        };
    }

    private static void CollectArticleIds(ArticleTreeDto article, HashSet<Guid> ids)
    {
        ids.Add(article.Id);
        if (article.Children != null)
        {
            foreach (var child in article.Children)
            {
                CollectArticleIds(child, ids);
            }
        }
    }

    /// <summary>
    /// Get a specific article by path in a public world.
    /// Returns null if article doesn't exist, world is not public, or article is not Public visibility.
    /// Path format: "article-slug/child-slug" (does not include world slug)
    /// </summary>
    public async Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath)
    {
        var normalizedSlug = _readAccessPolicy.NormalizePublicSlug(publicSlug);

        // First, verify the world exists and is public
        var world = await _readAccessPolicy
            .ApplyPublicWorldSlugFilter(_context.Worlds.AsNoTracking(), normalizedSlug)
            .Select(w => new { w.Id, w.Name, w.Slug })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogTraceSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        if (string.IsNullOrWhiteSpace(articlePath))
        {
            _logger.LogTraceSanitized("Empty article path for public world '{PublicSlug}'", normalizedSlug);
            return null;
        }

        var slugs = articlePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (slugs.Length == 0)
            return null;

        var resolvedArticle = await ArticleSlugPathResolver.ResolveAsync(
            slugs,
            async (slug, parentId, isRootLevel) =>
            {
                var query = _readAccessPolicy
                    .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), world.Id)
                    .Where(a => a.Slug == slug);

                query = isRootLevel
                    ? query.Where(a => a.ParentId == null)
                    : query.Where(a => a.ParentId == parentId);

                var article = await query
                    .Select(a => new { a.Id, a.Type })
                    .FirstOrDefaultAsync();

                return article == null
                    ? null
                    : (article.Id, article.Type);
            },
            (_, _, _) => Task.FromResult<(Guid Id, ArticleType Type)?>(null));

        if (resolvedArticle == null)
        {
            _logger.LogTraceSanitized("Public article not found for path '{Path}' in world '{PublicSlug}'",
                articlePath, normalizedSlug);
            return null;
        }

        // Found the article, now get full details
        var article = await _readAccessPolicy
            .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), world.Id)
            .Where(a => a.Id == resolvedArticle.Value.Id)
            .Select(ArticleReadModelProjection.ArticleDetail)
            .FirstOrDefaultAsync();

        if (article == null)
            return null;

        // Build breadcrumbs (only including public articles) using centralised hierarchy service
        article.Breadcrumbs = await _hierarchyService.BuildBreadcrumbsAsync(resolvedArticle.Value.Id, new HierarchyWalkOptions
        {
            PublicOnly = true,
            IncludeWorldBreadcrumb = true,
            IncludeVirtualGroups = true,
            World = new WorldContext { Id = world.Id, Name = world.Name, Slug = world.Slug }
        });

        _logger.LogTraceSanitized("Public article '{Title}' accessed in world '{PublicSlug}'",
            article.Title, normalizedSlug);

        return article;
    }

    /// <summary>
    /// Resolve an article ID to its public URL path.
    /// Returns null if the article doesn't exist, is not public, or doesn't belong to the specified world.
    /// </summary>
    public async Task<string?> GetPublicArticlePathAsync(string publicSlug, Guid articleId)
    {
        var normalizedSlug = _readAccessPolicy.NormalizePublicSlug(publicSlug);

        // Verify the world exists and is public
        var world = await _readAccessPolicy
            .ApplyPublicWorldSlugFilter(_context.Worlds.AsNoTracking(), normalizedSlug)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogTraceSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        // Get the article and verify it's public and belongs to this world
        var article = await _readAccessPolicy
            .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), world.Id)
            .Where(a => a.Id == articleId)
            .Select(a => new { a.Id, a.Slug, a.ParentId })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            _logger.LogTraceSanitized("Public article {ArticleId} not found in world '{PublicSlug}'", articleId, normalizedSlug);
            return null;
        }

        // Build the path by walking up the parent tree.
        var slugs = new List<string> { article.Slug };

        var currentParentId = article.ParentId;

        while (currentParentId.HasValue)
        {
            var parentArticle = await _readAccessPolicy
                .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), world.Id)
                .Where(a => a.Id == currentParentId.Value)
                .Select(a => new { a.Slug, a.ParentId })
                .FirstOrDefaultAsync();

            if (parentArticle == null)
            {
                // Parent is not public - this article's path is broken
                _logger.LogTraceSanitized("Parent article not public in chain for article {ArticleId}", articleId);
                return null;
            }

            slugs.Insert(0, parentArticle.Slug);
            currentParentId = parentArticle.ParentId;
        }

        var path = string.Join("/", slugs);
        _logger.LogTraceSanitized("Resolved article {ArticleId} to path '{Path}' in world '{PublicSlug}'",
            articleId, path, normalizedSlug);

        return path;
    }

    /// <summary>
    /// Resolve a public inline-image document ID to a fresh download URL.
    /// The document must be an image attached to a public article in a public world.
    /// </summary>
    public async Task<string?> GetPublicDocumentDownloadUrlAsync(Guid documentId)
    {
        var publicArticles = _readAccessPolicy
            .ApplyPublicVisibilityFilter(_context.Articles.AsNoTracking());
        var publicWorlds = _readAccessPolicy
            .ApplyPublicWorldFilter(_context.Worlds.AsNoTracking());

        var document = await _context.WorldDocuments
            .AsNoTracking()
            .Where(d => d.Id == documentId
                        && d.ArticleId.HasValue
                        && d.ContentType.StartsWith("image/"))
            .Join(
                publicArticles,
                d => d.ArticleId,
                a => a.Id,
                (d, a) => new
                {
                    d.BlobPath,
                    d.WorldId,
                    ArticleWorldId = a.WorldId
                })
            .Join(
                publicWorlds,
                joined => joined.WorldId,
                w => w.Id,
                (joined, _) => joined)
            .FirstOrDefaultAsync();

        if (document == null
            || document.ArticleWorldId != document.WorldId)
        {
            return null;
        }

        return await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);
    }

    /// <inheritdoc />
    public async Task<(GetBasemapReadUrlResponseDto? Basemap, string? Error)> GetPublicMapBasemapReadUrlAsync(string publicSlug, Guid mapId)
    {
        var map = await GetPublicMapAsync(publicSlug, mapId);
        if (map == null)
        {
            return (null, "Map not found or not public");
        }

        if (string.IsNullOrWhiteSpace(map.BasemapBlobKey))
        {
            return (null, "Basemap is missing for this map.");
        }

        var readUrl = await _mapBlobStore.GenerateReadSasUrlAsync(map.BasemapBlobKey);
        return (new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, null);
    }

    /// <inheritdoc />
    public async Task<List<MapLayerDto>?> GetPublicMapLayersAsync(string publicSlug, Guid mapId)
    {
        var map = await GetPublicMapAsync(publicSlug, mapId);
        if (map == null)
        {
            return null;
        }

        return await _context.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == mapId)
            .OrderBy(layer => layer.SortOrder)
            .Select(layer => new MapLayerDto
            {
                MapLayerId = layer.MapLayerId,
                ParentLayerId = layer.ParentLayerId,
                Name = layer.Name,
                SortOrder = layer.SortOrder,
                IsEnabled = layer.IsEnabled,
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MapPinResponseDto>?> GetPublicMapPinsAsync(string publicSlug, Guid mapId)
    {
        var map = await GetPublicMapAsync(publicSlug, mapId);
        if (map == null)
        {
            return null;
        }

        var pins = await _context.MapFeatures
            .AsNoTracking()
            .Where(feature => feature.WorldMapId == mapId && feature.FeatureType == MapFeatureType.Point)
            .OrderBy(feature => feature.MapFeatureId)
            .Select(feature => new
            {
                feature.MapFeatureId,
                feature.WorldMapId,
                feature.MapLayerId,
                feature.Name,
                X = feature.X,
                Y = feature.Y,
                feature.LinkedArticleId,
            })
            .ToListAsync();

        var linkedArticles = await GetPublicLinkedArticleTitlesAsync(
            map.WorldId,
            pins.Where(pin => pin.LinkedArticleId.HasValue).Select(pin => pin.LinkedArticleId!.Value));

        return pins
            .Select(pin => new MapPinResponseDto
            {
                PinId = pin.MapFeatureId,
                MapId = pin.WorldMapId,
                LayerId = pin.MapLayerId,
                Name = pin.Name,
                X = pin.X,
                Y = pin.Y,
                LinkedArticle = pin.LinkedArticleId.HasValue
                    && linkedArticles.TryGetValue(pin.LinkedArticleId.Value, out var title)
                    ? new LinkedArticleSummaryDto
                    {
                        ArticleId = pin.LinkedArticleId.Value,
                        Title = title,
                    }
                    : null,
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<MapFeatureDto>?> GetPublicMapFeaturesAsync(string publicSlug, Guid mapId)
    {
        var map = await GetPublicMapAsync(publicSlug, mapId);
        if (map == null)
        {
            return null;
        }

        var features = await _context.MapFeatures
            .AsNoTracking()
            .Where(feature => feature.WorldMapId == mapId)
            .OrderBy(feature => feature.MapFeatureId)
            .ToListAsync();

        return await ToPublicMapFeatureDtosAsync(map.WorldId, features);
    }

    private async Task<PublicMapLookup?> GetPublicMapAsync(string publicSlug, Guid mapId)
    {
        var normalizedSlug = _readAccessPolicy.NormalizePublicSlug(publicSlug);
        var worldId = await _readAccessPolicy
            .ApplyPublicWorldSlugFilter(_context.Worlds.AsNoTracking(), normalizedSlug)
            .Select(world => (Guid?)world.Id)
            .FirstOrDefaultAsync();

        if (!worldId.HasValue)
        {
            return null;
        }

        return await _context.WorldMaps
            .AsNoTracking()
            .Where(map => map.WorldId == worldId.Value && map.WorldMapId == mapId)
            .Select(map => new PublicMapLookup
            {
                WorldId = map.WorldId,
                BasemapBlobKey = map.BasemapBlobKey,
            })
            .FirstOrDefaultAsync();
    }

    private async Task<Dictionary<Guid, string>> GetPublicLinkedArticleTitlesAsync(Guid worldId, IEnumerable<Guid> linkedArticleIds)
    {
        var ids = linkedArticleIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await _readAccessPolicy
            .ApplyPublicArticleFilter(_context.Articles.AsNoTracking(), worldId)
            .Where(article => ids.Contains(article.Id))
            .ToDictionaryAsync(article => article.Id, article => article.Title);
    }

    private Task<Dictionary<Guid, string>> GetPublicLinkedArticleTitlesAsync(Guid worldId, IEnumerable<MapFeature> features)
    {
        return GetPublicLinkedArticleTitlesAsync(
            worldId,
            features
                .Where(feature => feature.LinkedArticleId.HasValue)
                .Select(feature => feature.LinkedArticleId!.Value));
    }

    private async Task<List<MapFeatureDto>> ToPublicMapFeatureDtosAsync(Guid worldId, IReadOnlyCollection<MapFeature> features)
    {
        var linkedArticles = await GetPublicLinkedArticleTitlesAsync(worldId, features);
        var results = new List<MapFeatureDto>(features.Count);

        foreach (var feature in features)
        {
            PolygonGeometryDto? polygon = null;
            if (feature.FeatureType == MapFeatureType.Polygon && !string.IsNullOrWhiteSpace(feature.GeometryBlobKey))
            {
                var geometryJson = await _mapBlobStore.LoadFeatureGeometryAsync(feature.GeometryBlobKey);
                polygon = geometryJson == null
                    ? null
                    : JsonSerializer.Deserialize<PolygonGeometryDto>(geometryJson, GeometryJsonOptions);
            }

            results.Add(new MapFeatureDto
            {
                FeatureId = feature.MapFeatureId,
                MapId = feature.WorldMapId,
                LayerId = feature.MapLayerId,
                FeatureType = feature.FeatureType,
                Name = feature.Name,
                Color = feature.Color,
                LinkedArticleId = feature.LinkedArticleId,
                LinkedArticle = feature.LinkedArticleId.HasValue
                    && linkedArticles.TryGetValue(feature.LinkedArticleId.Value, out var title)
                    ? new LinkedArticleSummaryDto
                    {
                        ArticleId = feature.LinkedArticleId.Value,
                        Title = title,
                    }
                    : null,
                Point = feature.FeatureType == MapFeatureType.Point
                    ? new MapFeaturePointDto
                    {
                        X = feature.X,
                        Y = feature.Y,
                    }
                    : null,
                Polygon = polygon,
                Geometry = feature.FeatureType == MapFeatureType.Polygon && !string.IsNullOrWhiteSpace(feature.GeometryBlobKey)
                    ? new MapFeatureGeometryReferenceDto
                    {
                        BlobKey = feature.GeometryBlobKey,
                        ETag = feature.GeometryETag,
                    }
                    : null,
            });
        }

        return results;
    }

    private sealed class PublicMapLookup
    {
        public Guid WorldId { get; init; }

        public string? BasemapBlobKey { get; init; }
    }
}
