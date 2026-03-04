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
