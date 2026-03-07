using Chronicis.Api.Data;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Manages WorldMaps: creation with default layers, metadata retrieval, and basemap uploads.
/// </summary>
public class WorldMapService : IWorldMapService
{
    private readonly ChronicisDbContext _db;
    private readonly IMapBlobStore _mapBlobStore;
    private readonly ILogger<WorldMapService> _logger;
    private const int MaxPinNameLength = 200;

    internal static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
    };

    public WorldMapService(
        ChronicisDbContext db,
        IMapBlobStore mapBlobStore,
        ILogger<WorldMapService> logger)
    {
        _db = db;
        _mapBlobStore = mapBlobStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MapDto> CreateMapAsync(Guid worldId, Guid userId, MapCreateDto dto)
    {
        _logger.LogTraceSanitized("User {UserId} creating map '{Name}' in world {WorldId}", userId, dto.Name, worldId);

        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }

        var map = new WorldMap
        {
            WorldMapId = Guid.NewGuid(),
            WorldId = worldId,
            Name = dto.Name,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

        _db.WorldMaps.Add(map);

        _db.MapLayers.AddRange(
            new MapLayer { MapLayerId = Guid.NewGuid(), WorldMapId = map.WorldMapId, Name = "World", SortOrder = 0, IsEnabled = false },
            new MapLayer { MapLayerId = Guid.NewGuid(), WorldMapId = map.WorldMapId, Name = "Campaign", SortOrder = 1, IsEnabled = false },
            new MapLayer { MapLayerId = Guid.NewGuid(), WorldMapId = map.WorldMapId, Name = "Arc", SortOrder = 2, IsEnabled = false });

        await _db.SaveChangesAsync();

        _logger.LogTraceSanitized("Created map {MapId} with 3 default layers", map.WorldMapId);

        return ToMapDto(map);
    }

    /// <inheritdoc/>
    public async Task<MapDto?> GetMapAsync(Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} getting map {MapId}", userId, mapId);

        var map = await _db.WorldMaps
            .Include(m => m.World)
                .ThenInclude(w => w.Members)
            .Include(m => m.WorldMapCampaigns)
            .Include(m => m.WorldMapArcs)
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId);

        if (map == null)
        {
            return null;
        }

        var hasAccess = map.World.OwnerId == userId
            || map.World.Members.Any(m => m.UserId == userId);

        if (!hasAccess)
        {
            return null;
        }

        return ToMapDto(map);
    }

    /// <inheritdoc/>
    public async Task<MapDto> UpdateMapAsync(Guid worldId, Guid mapId, Guid userId, MapUpdateDto dto)
    {
        _logger.LogTraceSanitized("User {UserId} updating map {MapId} in world {WorldId}", userId, mapId, worldId);

        var map = await _db.WorldMaps
            .Include(m => m.World)
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        if (map.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the world owner can update maps");
        }

        map.Name = dto.Name.Trim();
        map.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToMapDto(map);
    }

    /// <inheritdoc/>
    public async Task<List<MapSummaryDto>> ListMapsForWorldAsync(Guid worldId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} listing maps for world {WorldId}", userId, worldId);

        var hasAccess = await _db.Worlds
            .AsNoTracking()
            .AnyAsync(w => w.Id == worldId
                && (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)));

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }

        var maps = await _db.WorldMaps
            .AsNoTracking()
            .Include(m => m.WorldMapCampaigns)
            .Include(m => m.WorldMapArcs)
            .Where(m => m.WorldId == worldId)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return maps.Select(ToMapSummaryDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<MapAutocompleteDto>> SearchMapsForWorldAsync(Guid worldId, Guid userId, string? query)
    {
        _logger.LogTraceSanitized("User {UserId} searching map autocomplete for world {WorldId}", userId, worldId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var normalizedQuery = query?.Trim();
        var mapsQuery = _db.WorldMaps
            .AsNoTracking()
            .Where(m => m.WorldId == worldId);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var normalizedQueryLower = normalizedQuery.ToLower();
            mapsQuery = mapsQuery.Where(m => m.Name.ToLower().Contains(normalizedQueryLower));
        }

        return await mapsQuery
            .OrderBy(m => m.Name)
            .Select(m => new MapAutocompleteDto
            {
                MapId = m.WorldMapId,
                Name = m.Name,
            })
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<MapPinResponseDto> CreatePinAsync(Guid worldId, Guid mapId, Guid userId, MapPinCreateDto dto)
    {
        _logger.LogTraceSanitized("User {UserId} creating pin on map {MapId}", userId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);
        ValidateNormalizedCoordinates(dto.X, dto.Y);
        var pinName = NormalizePinName(dto.Name);

        var layerId = await ResolveDefaultLayerIdAsync(worldId, mapId);
        var pin = new MapFeature
        {
            MapFeatureId = Guid.NewGuid(),
            WorldMapId = mapId,
            MapLayerId = layerId,
            Name = pinName,
            X = dto.X,
            Y = dto.Y,
            LinkedArticleId = dto.LinkedArticleId,
        };

        _db.MapFeatures.Add(pin);
        await _db.SaveChangesAsync();

        return await GetMapPinResponseAsync(pin.MapFeatureId, mapId);
    }

    /// <inheritdoc/>
    public async Task<List<MapPinResponseDto>> ListPinsForMapAsync(Guid worldId, Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} listing pins for map {MapId}", userId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);
        await EnsureMapInWorldAsync(worldId, mapId);

        var pins = await _db.MapFeatures
            .AsNoTracking()
            .Where(mf => mf.WorldMapId == mapId)
            .OrderBy(mf => mf.MapFeatureId)
            .Select(mf => new
            {
                mf.MapFeatureId,
                mf.WorldMapId,
                mf.MapLayerId,
                mf.Name,
                X = mf.X,
                Y = mf.Y,
                mf.LinkedArticleId,
            })
            .ToListAsync();

        var linkedArticleIds = pins
            .Where(p => p.LinkedArticleId.HasValue)
            .Select(p => p.LinkedArticleId!.Value)
            .Distinct()
            .ToList();

        var linkedArticles = linkedArticleIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Articles
                .AsNoTracking()
                .Where(a => linkedArticleIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Title);

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

    /// <inheritdoc/>
    public async Task<MapPinResponseDto> UpdatePinPositionAsync(
        Guid worldId, Guid mapId, Guid pinId, Guid userId, MapPinPositionUpdateDto dto)
    {
        _logger.LogTraceSanitized("User {UserId} updating pin {PinId} on map {MapId}", userId, pinId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);
        await EnsureMapInWorldAsync(worldId, mapId);
        ValidateNormalizedCoordinates(dto.X, dto.Y);

        var pin = await _db.MapFeatures
            .FirstOrDefaultAsync(mf => mf.MapFeatureId == pinId && mf.WorldMapId == mapId);

        if (pin == null)
        {
            throw new InvalidOperationException("Pin not found");
        }

        pin.X = dto.X;
        pin.Y = dto.Y;

        await _db.SaveChangesAsync();

        return await GetMapPinResponseAsync(pin.MapFeatureId, mapId);
    }

    /// <inheritdoc/>
    public async Task DeletePinAsync(Guid worldId, Guid mapId, Guid pinId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} deleting pin {PinId} on map {MapId}", userId, pinId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);
        await EnsureMapInWorldAsync(worldId, mapId);

        var pin = await _db.MapFeatures
            .FirstOrDefaultAsync(mf => mf.MapFeatureId == pinId && mf.WorldMapId == mapId);

        if (pin == null)
        {
            throw new InvalidOperationException("Pin not found");
        }

        _db.MapFeatures.Remove(pin);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<RequestBasemapUploadResponseDto> RequestBasemapUploadAsync(
        Guid worldId, Guid mapId, Guid userId, RequestBasemapUploadDto dto)
    {
        _logger.LogTraceSanitized("User {UserId} requesting basemap upload for map {MapId}", userId, mapId);

        if (!AllowedContentTypes.Contains(dto.ContentType))
        {
            throw new ArgumentException(
                $"Content type '{dto.ContentType}' is not supported. Allowed: image/png, image/jpeg, image/webp");
        }

        var map = await _db.WorldMaps
            .Include(m => m.World)
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        if (map.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the world owner can upload basemap images");
        }

        var sasUrl = await _mapBlobStore.GenerateUploadSasUrlAsync(mapId, dto.FileName, dto.ContentType);
        var blobKey = _mapBlobStore.BuildBasemapBlobKey(mapId, dto.FileName);

        map.BasemapBlobKey = blobKey;
        map.BasemapOriginalFilename = dto.FileName;
        map.BasemapContentType = dto.ContentType;
        map.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogTraceSanitized("Stored blob key {BlobKey} for map {MapId}", blobKey, mapId);

        return new RequestBasemapUploadResponseDto { UploadUrl = sasUrl };
    }

    /// <inheritdoc/>
    public async Task<MapDto> ConfirmBasemapUploadAsync(Guid worldId, Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} confirming basemap upload for map {MapId}", userId, mapId);

        var map = await _db.WorldMaps
            .Include(m => m.World)
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        if (map.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the world owner can confirm basemap uploads");
        }

        if (map.BasemapBlobKey == null)
        {
            throw new InvalidOperationException("No basemap upload was requested for this map");
        }

        _logger.LogTraceSanitized("Confirmed basemap upload for map {MapId}", mapId);

        return ToMapDto(map);
    }

    /// <inheritdoc/>
    public async Task<GetBasemapReadUrlResponseDto> GetBasemapReadUrlAsync(Guid worldId, Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} requesting basemap read URL for map {MapId}", userId, mapId);

        var hasAccess = await _db.Worlds
            .AsNoTracking()
            .AnyAsync(w => w.Id == worldId
                && (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)));

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }

        var map = await _db.WorldMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        if (string.IsNullOrWhiteSpace(map.BasemapBlobKey))
        {
            throw new InvalidOperationException("Basemap not found for this map");
        }

        var readUrl = await _mapBlobStore.GenerateReadSasUrlAsync(map.BasemapBlobKey);
        return new GetBasemapReadUrlResponseDto { ReadUrl = readUrl };
    }

    /// <inheritdoc/>
    public async Task DeleteMapAsync(Guid worldId, Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} deleting map {MapId} in world {WorldId}", userId, mapId, worldId);

        var map = await _db.WorldMaps
            .Include(m => m.World)
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        if (map.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the world owner can delete maps");
        }

        await _mapBlobStore.DeleteMapFolderAsync(mapId);

        _db.WorldMaps.Remove(map);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureWorldMembershipAsync(Guid worldId, Guid userId)
    {
        var hasAccess = await _db.Worlds
            .AsNoTracking()
            .AnyAsync(w => w.Id == worldId
                && (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)));

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }
    }

    private async Task EnsureMapInWorldAsync(Guid worldId, Guid mapId)
    {
        var mapExists = await _db.WorldMaps
            .AsNoTracking()
            .AnyAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (!mapExists)
        {
            throw new InvalidOperationException("Map not found");
        }
    }

    private async Task<Guid> ResolveDefaultLayerIdAsync(Guid worldId, Guid mapId)
    {
        var scopeData = await _db.WorldMaps
            .AsNoTracking()
            .Where(m => m.WorldMapId == mapId && m.WorldId == worldId)
            .Select(m => new
            {
                HasArcScope = m.WorldMapArcs.Any(),
                HasCampaignScope = m.WorldMapCampaigns.Any(),
            })
            .FirstOrDefaultAsync();

        if (scopeData == null)
        {
            throw new InvalidOperationException("Map not found");
        }

        var defaultLayerName = scopeData.HasArcScope
            ? "Arc"
            : scopeData.HasCampaignScope
                ? "Campaign"
                : "World";

        var layer = await _db.MapLayers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.WorldMapId == mapId && l.Name == defaultLayerName);

        if (layer == null)
        {
            throw new InvalidOperationException($"Default layer '{defaultLayerName}' not found");
        }

        return layer.MapLayerId;
    }

    private static void ValidateNormalizedCoordinates(float x, float y)
    {
        if (!IsNormalizedCoordinate(x) || !IsNormalizedCoordinate(y))
        {
            throw new ArgumentException("X and Y must be normalized between 0 and 1");
        }
    }

    private static bool IsNormalizedCoordinate(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;

    private static string? NormalizePinName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxPinNameLength)
        {
            throw new ArgumentException($"Pin name must be {MaxPinNameLength} characters or fewer");
        }

        return trimmed;
    }

    private async Task<MapPinResponseDto> GetMapPinResponseAsync(Guid pinId, Guid mapId)
    {
        var pin = await _db.MapFeatures
            .AsNoTracking()
            .Where(mf => mf.MapFeatureId == pinId && mf.WorldMapId == mapId)
            .Select(mf => new
            {
                mf.MapFeatureId,
                mf.WorldMapId,
                mf.MapLayerId,
                mf.Name,
                mf.X,
                mf.Y,
                mf.LinkedArticleId,
            })
            .FirstOrDefaultAsync();

        if (pin == null)
        {
            throw new InvalidOperationException("Pin not found");
        }

        LinkedArticleSummaryDto? linkedArticle = null;
        if (pin.LinkedArticleId.HasValue)
        {
            var article = await _db.Articles
                .AsNoTracking()
                .Where(a => a.Id == pin.LinkedArticleId.Value)
                .Select(a => new { a.Id, a.Title })
                .FirstOrDefaultAsync();

            if (article != null)
            {
                linkedArticle = new LinkedArticleSummaryDto
                {
                    ArticleId = article.Id,
                    Title = article.Title,
                };
            }
        }

        return new MapPinResponseDto
        {
            PinId = pin.MapFeatureId,
            MapId = pin.WorldMapId,
            LayerId = pin.MapLayerId,
            Name = pin.Name,
            X = pin.X,
            Y = pin.Y,
            LinkedArticle = linkedArticle,
        };
    }

    internal static MapScope ComputeScope(WorldMap map)
    {
        if (map.WorldMapArcs.Any())
        {
            return MapScope.ArcScoped;
        }

        if (map.WorldMapCampaigns.Any())
        {
            return MapScope.CampaignScoped;
        }

        return MapScope.WorldScoped;
    }

    private static MapDto ToMapDto(WorldMap map) =>
        new()
        {
            WorldMapId = map.WorldMapId,
            WorldId = map.WorldId,
            Name = map.Name,
            HasBasemap = map.BasemapBlobKey != null,
            BasemapContentType = map.BasemapContentType,
            BasemapOriginalFilename = map.BasemapOriginalFilename,
            CreatedUtc = map.CreatedUtc,
            UpdatedUtc = map.UpdatedUtc,
        };

    private static MapSummaryDto ToMapSummaryDto(WorldMap map) =>
        new()
        {
            WorldMapId = map.WorldMapId,
            Name = map.Name,
            HasBasemap = map.BasemapBlobKey != null,
            Scope = ComputeScope(map),
            CampaignIds = map.WorldMapCampaigns.Select(c => c.CampaignId).ToList(),
            ArcIds = map.WorldMapArcs.Select(a => a.ArcId).ToList(),
        };
}
