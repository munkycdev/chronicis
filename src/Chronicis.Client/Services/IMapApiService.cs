using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for Map API operations.
/// </summary>
public interface IMapApiService
{
    /// <summary>
    /// Lists all maps for the given world.
    /// </summary>
    Task<List<MapSummaryDto>> ListMapsForWorldAsync(Guid worldId);

    /// <summary>
    /// Creates a map in the specified world.
    /// </summary>
    Task<MapDto?> CreateMapAsync(Guid worldId, MapCreateDto dto);

    /// <summary>
    /// Gets full map metadata with HTTP status details for page-state handling.
    /// </summary>
    Task<(MapDto? Map, int? StatusCode, string? Error)> GetMapAsync(Guid worldId, Guid mapId);

    /// <summary>
    /// Updates map metadata.
    /// </summary>
    Task<MapDto?> UpdateMapAsync(Guid worldId, Guid mapId, MapUpdateDto dto);

    /// <summary>
    /// Requests a basemap upload SAS URL for the map.
    /// </summary>
    Task<RequestBasemapUploadResponseDto?> RequestBasemapUploadAsync(
        Guid worldId,
        Guid mapId,
        RequestBasemapUploadDto dto);

    /// <summary>
    /// Confirms basemap upload completion.
    /// </summary>
    Task<MapDto?> ConfirmBasemapUploadAsync(
        Guid worldId,
        Guid mapId,
        string basemapBlobKey,
        string contentType,
        string originalFilename);

    /// <summary>
    /// Requests a short-lived read SAS URL for the map basemap.
    /// Includes HTTP status details for page-state handling.
    /// </summary>
    Task<(GetBasemapReadUrlResponseDto? Basemap, int? StatusCode, string? Error)> GetBasemapReadUrlAsync(
        Guid worldId,
        Guid mapId);

    /// <summary>
    /// Permanently deletes a map and all associated data.
    /// </summary>
    Task<bool> DeleteMapAsync(Guid worldId, Guid mapId);
}
