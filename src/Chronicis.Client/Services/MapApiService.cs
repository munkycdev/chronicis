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
