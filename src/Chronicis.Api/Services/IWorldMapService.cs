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
    /// List layers for a map ordered by sort order.
    /// Requires world membership.
    /// </summary>
    Task<List<MapLayerDto>> ListLayersForMapAsync(Guid worldId, Guid mapId, Guid userId);

    /// <summary>
    /// Create a custom layer for a map.
    /// Requires world membership.
    /// </summary>
    Task<MapLayerDto> CreateLayer(Guid worldId, Guid mapId, Guid userId, string name);

    /// <summary>
    /// Update editable map metadata. Only the world owner may update map details.
    /// </summary>
    Task<MapDto> UpdateMapAsync(Guid worldId, Guid mapId, Guid userId, MapUpdateDto dto);

    /// <summary>
    /// List all maps for a world, sorted by name, with scope data for grouping.
    /// Throws <see cref="UnauthorizedAccessException"/> if the user has no access.
    /// </summary>
    Task<List<MapSummaryDto>> ListMapsForWorldAsync(Guid worldId, Guid userId);

    /// <summary>
    /// List map suggestions for editor autocomplete.
    /// Returns minimal data and supports optional name filtering.
    /// Requires world membership.
    /// </summary>
    Task<List<MapAutocompleteDto>> SearchMapsForWorldAsync(Guid worldId, Guid userId, string? query);

    /// <summary>
    /// Create a pin for a map, selecting the default layer using Arc > Campaign > World.
    /// Requires world membership.
    /// </summary>
    Task<MapPinResponseDto> CreatePinAsync(Guid worldId, Guid mapId, Guid userId, MapPinCreateDto dto);

    /// <summary>
    /// List pins for a map.
    /// Requires world membership.
    /// </summary>
    Task<List<MapPinResponseDto>> ListPinsForMapAsync(Guid worldId, Guid mapId, Guid userId);

    /// <summary>
    /// Update a pin position.
    /// Requires world membership.
    /// </summary>
    Task<MapPinResponseDto> UpdatePinPositionAsync(
        Guid worldId, Guid mapId, Guid pinId, Guid userId, MapPinPositionUpdateDto dto);

    /// <summary>
    /// Update map layer visibility.
    /// Requires world membership.
    /// </summary>
    Task UpdateLayerVisibility(
        Guid worldId, Guid mapId, Guid layerId, Guid userId, bool isEnabled);

    /// <summary>
    /// Reorder map layers using the provided full ordered set of layer IDs.
    /// Requires world membership.
    /// </summary>
    Task ReorderLayers(Guid worldId, Guid mapId, Guid userId, IList<Guid> layerIds);

    /// <summary>
    /// Rename a map layer.
    /// Requires world membership.
    /// </summary>
    Task RenameLayer(Guid worldId, Guid mapId, Guid userId, Guid layerId, string name);

    /// <summary>
    /// Delete a map layer.
    /// Requires world membership.
    /// </summary>
    Task DeleteLayer(Guid worldId, Guid mapId, Guid userId, Guid layerId);

    /// <summary>
    /// Delete a pin from a map.
    /// Requires world membership.
    /// </summary>
    Task DeletePinAsync(Guid worldId, Guid mapId, Guid pinId, Guid userId);

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

    /// <summary>
    /// Permanently deletes a map, all related metadata, and all blobs under the map folder.
    /// Only the world owner may delete maps.
    /// </summary>
    Task DeleteMapAsync(Guid worldId, Guid mapId, Guid userId);
}
