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
    private const int MaxLayerNameLength = 200;
    private static readonly HashSet<string> ProtectedDefaultLayerNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "World",
        "Campaign",
        "Arc",
    };

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
    public async Task<List<MapLayerDto>> ListLayersForMapAsync(Guid worldId, Guid mapId, Guid userId)
    {
        _logger.LogTraceSanitized("User {UserId} listing layers for map {MapId}", userId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);
        await EnsureMapInWorldAsync(worldId, mapId);

        return await _db.MapLayers
            .AsNoTracking()
            .Where(l => l.WorldMapId == mapId)
            .OrderBy(l => l.SortOrder)
            .Select(l => new MapLayerDto
            {
                MapLayerId = l.MapLayerId,
                ParentLayerId = l.ParentLayerId,
                Name = l.Name,
                SortOrder = l.SortOrder,
                IsEnabled = l.IsEnabled,
            })
            .ToListAsync();
    }

    Task<MapLayerDto> IWorldMapService.CreateLayer(Guid worldId, Guid mapId, Guid userId, string name, Guid? parentLayerId) =>
        CreateLayerAsync(worldId, mapId, userId, name, parentLayerId);

    public async Task<MapLayerDto> CreateLayerAsync(Guid worldId, Guid mapId, Guid userId, string name, Guid? parentLayerId = null)
    {
        _logger.LogTraceSanitized("User {UserId} creating layer on map {MapId}", userId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var map = await _db.WorldMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(existingMap => existingMap.WorldMapId == mapId && existingMap.WorldId == worldId);

        if (map == null)
        {
            throw new ArgumentException("Map not found");
        }

        var normalizedName = NormalizeLayerName(name);
        var normalizedNameLower = normalizedName.ToLowerInvariant();
        var hasDuplicateName = await _db.MapLayers
            .AsNoTracking()
            .AnyAsync(layer =>
                layer.WorldMapId == mapId
                && layer.Name.ToLower() == normalizedNameLower);

        if (hasDuplicateName)
        {
            throw new ArgumentException("Layer name already exists");
        }

        if (parentLayerId.HasValue)
        {
            var parentLayer = await _db.MapLayers
                .AsNoTracking()
                .FirstOrDefaultAsync(layer => layer.MapLayerId == parentLayerId.Value);

            if (parentLayer == null)
            {
                throw new ArgumentException("Parent layer not found");
            }

            if (parentLayer.WorldMapId != mapId)
            {
                throw new ArgumentException("Parent layer does not belong to map");
            }
        }

        var nextSortOrder = await _db.MapLayers
            .AsNoTracking()
            .Where(layer => layer.WorldMapId == mapId && layer.ParentLayerId == parentLayerId)
            .Select(layer => (int?)layer.SortOrder)
            .MaxAsync() ?? -1;

        var createdLayer = new MapLayer
        {
            MapLayerId = Guid.NewGuid(),
            WorldMapId = map.WorldMapId,
            ParentLayerId = parentLayerId,
            Name = normalizedName,
            SortOrder = nextSortOrder + 1,
            IsEnabled = true,
        };

        _db.MapLayers.Add(createdLayer);
        await _db.SaveChangesAsync();

        return new MapLayerDto
        {
            MapLayerId = createdLayer.MapLayerId,
            ParentLayerId = createdLayer.ParentLayerId,
            Name = createdLayer.Name,
            SortOrder = createdLayer.SortOrder,
            IsEnabled = createdLayer.IsEnabled,
        };
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

        Guid layerId;
        if (dto.LayerId.HasValue)
        {
            await EnsureMapInWorldAsync(worldId, mapId);

            var layer = await _db.MapLayers
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.MapLayerId == dto.LayerId.Value);

            if (layer == null)
            {
                throw new ArgumentException("Layer not found");
            }

            if (layer.WorldMapId != mapId)
            {
                throw new ArgumentException("Layer does not belong to map");
            }

            layerId = layer.MapLayerId;
        }
        else
        {
            layerId = await ResolveDefaultLayerIdAsync(worldId, mapId);
        }

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

    Task IWorldMapService.UpdateLayerVisibility(
        Guid worldId, Guid mapId, Guid layerId, Guid userId, bool isEnabled) =>
        UpdateLayerVisibilityAsync(worldId, mapId, layerId, userId, isEnabled);

    public async Task UpdateLayerVisibilityAsync(
        Guid worldId, Guid mapId, Guid layerId, Guid userId, bool isEnabled)
    {
        _logger.LogTraceSanitized(
            "User {UserId} updating layer visibility for layer {LayerId} on map {MapId}",
            userId,
            layerId,
            mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var map = await _db.WorldMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (map == null)
        {
            throw new ArgumentException("Map not found");
        }

        var layer = await _db.MapLayers
            .FirstOrDefaultAsync(l => l.MapLayerId == layerId);

        if (layer == null)
        {
            throw new ArgumentException("Layer not found");
        }

        if (layer.WorldMapId != map.WorldMapId)
        {
            throw new ArgumentException("Layer does not belong to map");
        }

        layer.IsEnabled = isEnabled;

        await _db.SaveChangesAsync();
    }

    Task IWorldMapService.ReorderLayers(Guid worldId, Guid mapId, Guid userId, IList<Guid> layerIds) =>
        ReorderLayersAsync(worldId, mapId, userId, layerIds);

    private async Task ReorderLayersAsync(Guid worldId, Guid mapId, Guid userId, IList<Guid> layerIds)
    {
        _logger.LogTraceSanitized("User {UserId} reordering layers on map {MapId}", userId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        if (layerIds == null || layerIds.Count == 0)
        {
            throw new ArgumentException("LayerIds are required");
        }

        var mapExists = await _db.WorldMaps
            .AsNoTracking()
            .AnyAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (!mapExists)
        {
            throw new ArgumentException("Map not found");
        }

        if (layerIds.Distinct().Count() != layerIds.Count)
        {
            throw new ArgumentException("Duplicate layer IDs are not allowed");
        }

        var requestedLayers = await _db.MapLayers
            .Where(layer => layerIds.Contains(layer.MapLayerId))
            .ToListAsync();

        if (requestedLayers.Count != layerIds.Count)
        {
            throw new ArgumentException("Layer not found");
        }

        if (requestedLayers.Any(layer => layer.WorldMapId != mapId))
        {
            throw new ArgumentException("Layer does not belong to map");
        }

        var parentLayerIds = requestedLayers
            .Select(layer => layer.ParentLayerId)
            .Distinct()
            .ToList();

        if (parentLayerIds.Count != 1)
        {
            throw new ArgumentException("Layers must share the same parent");
        }

        var parentLayerId = parentLayerIds[0];
        var siblingLayerIds = await _db.MapLayers
            .Where(layer => layer.WorldMapId == mapId && layer.ParentLayerId == parentLayerId)
            .Select(layer => layer.MapLayerId)
            .ToListAsync();

        if (siblingLayerIds.Count != layerIds.Count || !siblingLayerIds.ToHashSet().SetEquals(layerIds))
        {
            throw new ArgumentException("LayerIds must include all sibling layers");
        }

        var mapLayersById = requestedLayers.ToDictionary(layer => layer.MapLayerId);
        for (var index = 0; index < layerIds.Count; index++)
        {
            mapLayersById[layerIds[index]].SortOrder = index;
        }

        await _db.SaveChangesAsync();
    }

    Task IWorldMapService.RenameLayer(Guid worldId, Guid mapId, Guid userId, Guid layerId, string name) =>
        RenameLayerAsync(worldId, mapId, userId, layerId, name);

    public async Task RenameLayerAsync(Guid worldId, Guid mapId, Guid userId, Guid layerId, string name)
    {
        _logger.LogTraceSanitized("User {UserId} renaming layer {LayerId} on map {MapId}", userId, layerId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var mapExists = await _db.WorldMaps
            .AsNoTracking()
            .AnyAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (!mapExists)
        {
            throw new ArgumentException("Map not found");
        }

        var layer = await _db.MapLayers
            .FirstOrDefaultAsync(l => l.MapLayerId == layerId);

        if (layer == null)
        {
            throw new ArgumentException("Layer not found");
        }

        if (layer.WorldMapId != mapId)
        {
            throw new ArgumentException("Layer does not belong to map");
        }

        if (IsProtectedDefaultLayer(layer.Name))
        {
            throw new ArgumentException("Default layers cannot be renamed");
        }

        layer.Name = NormalizeLayerName(name);

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task SetLayerParentAsync(Guid worldId, Guid mapId, Guid userId, Guid layerId, Guid? parentLayerId)
    {
        _logger.LogTraceSanitized("User {UserId} setting parent for layer {LayerId} on map {MapId}", userId, layerId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var mapExists = await _db.WorldMaps
            .AsNoTracking()
            .AnyAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (!mapExists)
        {
            throw new ArgumentException("Map not found");
        }

        var layer = await _db.MapLayers
            .FirstOrDefaultAsync(l => l.MapLayerId == layerId && l.WorldMapId == mapId);

        if (layer == null)
        {
            throw new ArgumentException("Layer not found");
        }

        if (parentLayerId == layerId)
        {
            throw new ArgumentException("Layer cannot be its own parent");
        }

        if (layer.ParentLayerId == parentLayerId)
        {
            return;
        }

        if (IsProtectedDefaultLayer(layer.Name))
        {
            throw new ArgumentException("Default layers cannot be re-parented");
        }

        if (parentLayerId == null)
        {
            layer.ParentLayerId = null;
            await _db.SaveChangesAsync();
            return;
        }

        var parent = await _db.MapLayers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.MapLayerId == parentLayerId.Value && l.WorldMapId == mapId);

        if (parent == null)
        {
            throw new ArgumentException("Parent layer not found in map");
        }

        await EnsureValidParentChainAsync(mapId, layerId, parent.MapLayerId);

        layer.ParentLayerId = parent.MapLayerId;
        await _db.SaveChangesAsync();
    }

    Task IWorldMapService.DeleteLayer(Guid worldId, Guid mapId, Guid userId, Guid layerId) =>
        DeleteLayerAsync(worldId, mapId, userId, layerId);

    public async Task DeleteLayerAsync(Guid worldId, Guid mapId, Guid userId, Guid layerId)
    {
        _logger.LogTraceSanitized("User {UserId} deleting layer {LayerId} on map {MapId}", userId, layerId, mapId);

        await EnsureWorldMembershipAsync(worldId, userId);

        var mapExists = await _db.WorldMaps
            .AsNoTracking()
            .AnyAsync(m => m.WorldMapId == mapId && m.WorldId == worldId);

        if (!mapExists)
        {
            throw new ArgumentException("Map not found");
        }

        var layer = await _db.MapLayers
            .FirstOrDefaultAsync(l => l.MapLayerId == layerId);

        if (layer == null)
        {
            throw new ArgumentException("Layer not found");
        }

        if (layer.WorldMapId != mapId)
        {
            throw new ArgumentException("Layer does not belong to map");
        }

        if (IsProtectedDefaultLayer(layer.Name))
        {
            throw new ArgumentException("Default layers cannot be deleted");
        }

        var hasChildren = await _db.MapLayers
            .AsNoTracking()
            .AnyAsync(existingLayer =>
                existingLayer.WorldMapId == layer.WorldMapId
                && existingLayer.ParentLayerId == layer.MapLayerId);

        if (hasChildren)
        {
            throw new ArgumentException("Layer cannot be deleted while child layers exist");
        }

        var hasPins = await _db.MapFeatures
            .AsNoTracking()
            .AnyAsync(feature => feature.MapLayerId == layerId);

        if (hasPins)
        {
            throw new ArgumentException("Layer cannot be deleted while pins reference it");
        }

        var siblingParentLayerId = layer.ParentLayerId;
        var siblingWorldMapId = layer.WorldMapId;

        _db.MapLayers.Remove(layer);

        var remainingSiblingLayers = await _db.MapLayers
            .Where(existingLayer =>
                existingLayer.WorldMapId == siblingWorldMapId
                && existingLayer.ParentLayerId == siblingParentLayerId
                && existingLayer.MapLayerId != layerId)
            .OrderBy(existingLayer => existingLayer.SortOrder)
            .ThenBy(existingLayer => existingLayer.MapLayerId)
            .ToListAsync();

        for (var index = 0; index < remainingSiblingLayers.Count; index++)
        {
            remainingSiblingLayers[index].SortOrder = index;
        }

        await _db.SaveChangesAsync();
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

    private static string NormalizeLayerName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Layer name is required");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLayerNameLength)
        {
            throw new ArgumentException($"Layer name must be {MaxLayerNameLength} characters or fewer");
        }

        return trimmed;
    }

    private static bool IsProtectedDefaultLayer(string layerName) =>
        ProtectedDefaultLayerNames.Contains(layerName);

    private async Task EnsureValidParentChainAsync(Guid mapId, Guid layerId, Guid parentLayerId)
    {
        var visited = new HashSet<Guid>();
        Guid? currentLayerId = parentLayerId;

        while (currentLayerId.HasValue)
        {
            if (!visited.Add(currentLayerId.Value))
            {
                throw new ArgumentException("Invalid parent chain");
            }

            if (currentLayerId.Value == layerId)
            {
                throw new ArgumentException("Parent assignment would create a cycle");
            }

            var currentLayer = await _db.MapLayers
                .AsNoTracking()
                .Where(l => l.WorldMapId == mapId && l.MapLayerId == currentLayerId.Value)
                .Select(l => new
                {
                    l.MapLayerId,
                    l.ParentLayerId,
                })
                .FirstOrDefaultAsync();

            if (currentLayer == null)
            {
                throw new ArgumentException("Invalid parent chain");
            }

            currentLayerId = currentLayer.ParentLayerId;
        }
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
