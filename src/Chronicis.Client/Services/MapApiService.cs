using System.Net.Http.Json;
using System.Text.Json;
using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for Map API operations.
/// Uses HttpClientExtensions for consistent error handling and logging.
/// </summary>
public class MapApiService : IMapApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<MapApiService> _logger;

    public MapApiService(HttpClient http, ILogger<MapApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<MapSummaryDto>> ListMapsForWorldAsync(Guid worldId)
    {
        return await _http.GetListAsync<MapSummaryDto>(
            $"world/{worldId}/maps",
            _logger,
            $"maps for world {worldId}");
    }

    public async Task<List<MapAutocompleteDto>> GetMapAutocompleteAsync(Guid worldId, string? query)
    {
        var route = $"world/{worldId}/maps/autocomplete";
        if (!string.IsNullOrWhiteSpace(query))
        {
            route = $"{route}?query={Uri.EscapeDataString(query)}";
        }

        return await _http.GetListAsync<MapAutocompleteDto>(
            route,
            _logger,
            $"map autocomplete for world {worldId}");
    }

    public async Task<List<MapFeatureAutocompleteDto>> GetMapFeatureAutocompleteAsync(Guid worldId, string? query)
    {
        var route = $"world/{worldId}/maps/features/autocomplete";
        if (!string.IsNullOrWhiteSpace(query))
        {
            route = $"{route}?query={Uri.EscapeDataString(query)}";
        }

        return await _http.GetListAsync<MapFeatureAutocompleteDto>(
            route,
            _logger,
            $"map feature autocomplete for world {worldId}");
    }

    public async Task<MapDto?> CreateMapAsync(Guid worldId, MapCreateDto dto)
    {
        return await _http.PostEntityAsync<MapDto>(
            $"world/{worldId}/maps",
            dto,
            _logger,
            $"map in world {worldId}");
    }

    public async Task<(MapDto? Map, int? StatusCode, string? Error)> GetMapAsync(Guid worldId, Guid mapId)
    {
        return await GetEntityWithStatusAsync<MapDto>(
            $"world/{worldId}/maps/{mapId}",
            $"map {mapId}");
    }

    public async Task<List<MapLayerDto>> GetLayersForMapAsync(Guid worldId, Guid mapId)
    {
        return await _http.GetListAsync<MapLayerDto>(
            $"world/{worldId}/maps/{mapId}/layers",
            _logger,
            $"layers for map {mapId}");
    }

    public async Task<MapLayerDto> CreateLayerAsync(Guid worldId, Guid mapId, string name, Guid? parentLayerId = null)
    {
        var request = new CreateLayerRequest
        {
            Name = name,
            ParentLayerId = parentLayerId,
        };

        var createdLayer = await _http.PostEntityAsync<MapLayerDto>(
            $"world/{worldId}/maps/{mapId}/layers",
            request,
            _logger,
            $"layer for map {mapId}");

        if (createdLayer == null)
        {
            throw new InvalidOperationException("Failed to create layer");
        }

        return createdLayer;
    }

    public async Task<List<MapPinResponseDto>> ListPinsForMapAsync(Guid worldId, Guid mapId)
    {
        return await _http.GetListAsync<MapPinResponseDto>(
            $"world/{worldId}/maps/{mapId}/pins",
            _logger,
            $"pins for map {mapId}");
    }

    public async Task<MapPinResponseDto?> CreatePinAsync(Guid worldId, Guid mapId, MapPinCreateDto dto)
    {
        return await _http.PostEntityAsync<MapPinResponseDto>(
            $"world/{worldId}/maps/{mapId}/pins",
            dto,
            _logger,
            $"pin for map {mapId}");
    }

    public async Task<MapFeatureDto?> CreateFeatureAsync(Guid worldId, Guid mapId, MapFeatureCreateDto dto)
    {
        return await _http.PostEntityAsync<MapFeatureDto>(
            $"world/{worldId}/maps/{mapId}/features",
            dto,
            _logger,
            $"feature for map {mapId}");
    }

    public async Task<bool> DeletePinAsync(Guid worldId, Guid mapId, Guid pinId)
    {
        return await _http.DeleteEntityAsync(
            $"world/{worldId}/maps/{mapId}/pins/{pinId}",
            _logger,
            $"pin {pinId} for map {mapId}");
    }

    public async Task<bool> DeleteFeatureAsync(Guid worldId, Guid mapId, Guid featureId)
    {
        return await _http.DeleteEntityAsync(
            $"world/{worldId}/maps/{mapId}/features/{featureId}",
            _logger,
            $"feature {featureId} for map {mapId}");
    }

    public async Task<bool> UpdatePinPositionAsync(Guid worldId, Guid mapId, Guid pinId, MapPinPositionUpdateDto dto)
    {
        return await _http.PatchEntityAsync(
            $"world/{worldId}/maps/{mapId}/pins/{pinId}",
            dto,
            _logger,
            $"pin {pinId} for map {mapId}");
    }

    public async Task<List<MapFeatureDto>> ListFeaturesForMapAsync(Guid worldId, Guid mapId)
    {
        return await _http.GetListAsync<MapFeatureDto>(
            $"world/{worldId}/maps/{mapId}/features",
            _logger,
            $"features for map {mapId}");
    }

    public async Task<(MapFeatureDto? Feature, int? StatusCode, string? Error)> GetFeatureAsync(Guid worldId, Guid mapId, Guid featureId)
    {
        return await GetEntityWithStatusAsync<MapFeatureDto>(
            $"world/{worldId}/maps/{mapId}/features/{featureId}",
            $"feature {featureId} for map {mapId}");
    }

    public async Task<List<MapFeatureSessionReferenceDto>> GetFeatureSessionReferencesAsync(Guid worldId, Guid mapId, Guid featureId)
    {
        return await _http.GetListAsync<MapFeatureSessionReferenceDto>(
            $"world/{worldId}/maps/{mapId}/features/{featureId}/session-references",
            _logger,
            $"session references for feature {featureId} on map {mapId}");
    }

    public async Task<MapFeatureDto?> UpdateFeatureAsync(Guid worldId, Guid mapId, Guid featureId, MapFeatureUpdateDto dto)
    {
        return await _http.PutEntityAsync<MapFeatureDto>(
            $"world/{worldId}/maps/{mapId}/features/{featureId}",
            dto,
            _logger,
            $"feature {featureId} for map {mapId}");
    }

    public async Task<MapDto?> UpdateMapAsync(Guid worldId, Guid mapId, MapUpdateDto dto)
    {
        return await _http.PutEntityAsync<MapDto>(
            $"world/{worldId}/maps/{mapId}",
            dto,
            _logger,
            $"map {mapId}");
    }

    public async Task UpdateLayerVisibilityAsync(Guid worldId, Guid mapId, Guid layerId, bool isEnabled)
    {
        var request = new UpdateLayerVisibilityRequest
        {
            IsEnabled = isEnabled,
        };

        _ = await _http.PutBoolAsync(
            $"world/{worldId}/maps/{mapId}/layers/{layerId}",
            request,
            _logger,
            $"layer {layerId} visibility for map {mapId}");
    }

    public async Task ReorderLayersAsync(Guid worldId, Guid mapId, IList<Guid> layerIds)
    {
        var request = new ReorderLayersRequest
        {
            LayerIds = layerIds,
        };

        _ = await _http.PutBoolAsync(
            $"world/{worldId}/maps/{mapId}/layers/reorder",
            request,
            _logger,
            $"layer reorder for map {mapId}");
    }

    public async Task RenameLayerAsync(Guid worldId, Guid mapId, Guid layerId, string name)
    {
        var request = new RenameLayerRequest
        {
            Name = name,
        };

        var success = await _http.PutBoolAsync(
            $"world/{worldId}/maps/{mapId}/layers/{layerId}/rename",
            request,
            _logger,
            $"rename layer {layerId} on map {mapId}");

        if (!success)
        {
            throw new InvalidOperationException("Failed to rename layer");
        }
    }

    public async Task SetLayerParentAsync(Guid worldId, Guid mapId, Guid layerId, Guid? parentLayerId)
    {
        var request = new SetLayerParentRequest
        {
            ParentLayerId = parentLayerId,
        };

        var success = await _http.PutBoolAsync(
            $"world/{worldId}/maps/{mapId}/layers/{layerId}/parent",
            request,
            _logger,
            $"set parent for layer {layerId} on map {mapId}");

        if (!success)
        {
            throw new InvalidOperationException("Failed to set layer parent");
        }
    }

    public async Task DeleteLayerAsync(Guid worldId, Guid mapId, Guid layerId)
    {
        var success = await _http.DeleteEntityAsync(
            $"world/{worldId}/maps/{mapId}/layers/{layerId}",
            _logger,
            $"delete layer {layerId} on map {mapId}");

        if (!success)
        {
            throw new InvalidOperationException("Failed to delete layer");
        }
    }

    public async Task<RequestBasemapUploadResponseDto?> RequestBasemapUploadAsync(
        Guid worldId,
        Guid mapId,
        RequestBasemapUploadDto dto)
    {
        return await _http.PostEntityAsync<RequestBasemapUploadResponseDto>(
            $"world/{worldId}/maps/{mapId}/request-basemap-upload",
            dto,
            _logger,
            $"basemap upload request for map {mapId}");
    }

    public async Task<MapDto?> ConfirmBasemapUploadAsync(
        Guid worldId,
        Guid mapId,
        string basemapBlobKey,
        string contentType,
        string originalFilename)
    {
        var dto = new
        {
            BasemapBlobKey = basemapBlobKey,
            ContentType = contentType,
            OriginalFilename = originalFilename
        };

        return await _http.PostEntityAsync<MapDto>(
            $"world/{worldId}/maps/{mapId}/confirm-basemap-upload",
            dto,
            _logger,
            $"basemap confirm for map {mapId}");
    }

    public async Task<(GetBasemapReadUrlResponseDto? Basemap, int? StatusCode, string? Error)> GetBasemapReadUrlAsync(
        Guid worldId,
        Guid mapId)
    {
        return await GetEntityWithStatusAsync<GetBasemapReadUrlResponseDto>(
            $"world/{worldId}/maps/{mapId}/basemap",
            $"basemap read URL for map {mapId}");
    }

    public async Task<bool> DeleteMapAsync(Guid worldId, Guid mapId)
    {
        return await _http.DeleteEntityAsync(
            $"world/{worldId}/maps/{mapId}",
            _logger,
            $"map {mapId}");
    }

    private async Task<(T? Entity, int? StatusCode, string? Error)> GetEntityWithStatusAsync<T>(
        string url,
        string description) where T : class
    {
        try
        {
            var response = await _http.GetAsync(url);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var error = await TryReadErrorMessageAsync(response);
                _logger.LogWarning("Failed to fetch {Description}: {StatusCode}", description, response.StatusCode);
                return (null, statusCode, error);
            }

            var entity = await response.Content.ReadFromJsonAsync<T>();
            return (entity, statusCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Description} from {Url}", description, url);
            return (null, null, ex.Message);
        }
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var payload = JsonDocument.Parse(content);
            if (payload.RootElement.ValueKind == JsonValueKind.Object
                && payload.RootElement.TryGetProperty("error", out var errorProperty)
                && errorProperty.ValueKind == JsonValueKind.String)
            {
                return errorProperty.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
