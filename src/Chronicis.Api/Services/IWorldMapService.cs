using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing WorldMaps, default layers, and basemap uploads.
/// </summary>
public interface IWorldMapService
{
    /// <summary>
    /// Create a new map with three default hidden layers (World, Campaign, Arc).
    /// Only the world owner may create maps.
    /// </summary>
    Task<MapDto> CreateMapAsync(Guid worldId, Guid userId, MapCreateDto dto);

    /// <summary>
    /// Get full map metadata. Returns null if the map does not exist or the user
    /// has no access to the world.
    /// </summary>
    Task<MapDto?> GetMapAsync(Guid mapId, Guid userId);

    /// <summary>
    /// List all maps for a world, sorted by name, with scope data for grouping.
    /// Throws <see cref="UnauthorizedAccessException"/> if the user has no access.
    /// </summary>
    Task<List<MapSummaryDto>> ListMapsForWorldAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Validate the upload request, persist the blob key / filename / content-type,
    /// and return a short-lived SAS URL for a direct client-to-blob upload.
    /// Only the world owner may upload basemaps.
    /// </summary>
    Task<RequestBasemapUploadResponseDto> RequestBasemapUploadAsync(
        Guid worldId, Guid mapId, Guid userId, RequestBasemapUploadDto dto);

    /// <summary>
    /// Confirm that the client has finished uploading the basemap.
    /// Returns the updated map DTO.
    /// Only the world owner may confirm uploads.
    /// </summary>
    Task<MapDto> ConfirmBasemapUploadAsync(Guid worldId, Guid mapId, Guid userId);

    /// <summary>
    /// Returns a short-lived SAS read URL for the map basemap.
    /// Enforces world membership.
    /// </summary>
    Task<GetBasemapReadUrlResponseDto> GetBasemapReadUrlAsync(Guid worldId, Guid mapId, Guid userId);
}
