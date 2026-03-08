using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs.Maps;

/// <summary>
/// Request DTO for creating a new WorldMap.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapCreateDto
{
    /// <summary>
    /// Display name for the map.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating a WorldMap.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapUpdateDto
{
    /// <summary>
    /// Updated display name for the map.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Full metadata DTO for a WorldMap.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapDto
{
    public Guid WorldMapId { get; set; }
    public Guid WorldId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True if a basemap image has been uploaded.
    /// </summary>
    public bool HasBasemap { get; set; }

    public string? BasemapContentType { get; set; }
    public string? BasemapOriginalFilename { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

/// <summary>
/// Summary DTO used in the Maps list. Includes scope data so clients can group
/// maps by world / campaign / arc in P0-09 without an extra round-trip.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapSummaryDto
{
    public Guid WorldMapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool HasBasemap { get; set; }

    /// <summary>
    /// Computed scope: WorldScoped, CampaignScoped, or ArcScoped.
    /// Arc associations take priority over campaign associations.
    /// </summary>
    public MapScope Scope { get; set; }

    /// <summary>
    /// Campaign IDs this map is associated with (populated for CampaignScoped maps).
    /// </summary>
    public List<Guid> CampaignIds { get; set; } = [];

    /// <summary>
    /// Arc IDs this map is associated with (populated for ArcScoped maps).
    /// </summary>
    public List<Guid> ArcIds { get; set; } = [];
}

/// <summary>
/// Minimal map DTO for editor autocomplete lookups.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapAutocompleteDto
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Layer DTO used for map layer listing and visibility state.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapLayerDto
{
    public Guid MapLayerId { get; set; }
    public Guid? ParentLayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request DTO for creating a custom map layer.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateLayerRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentLayerId { get; set; }
}

/// <summary>
/// Request DTO for creating a pin on a map.
/// Coordinates are normalized to the 0..1 range.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapPinCreateDto
{
    public string? Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public Guid? LinkedArticleId { get; set; }
    public Guid? LayerId { get; set; }
}

/// <summary>
/// Minimal linked article metadata for map pin responses.
/// </summary>
[ExcludeFromCodeCoverage]
public class LinkedArticleSummaryDto
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO used for map pin create/list/update responses.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapPinResponseDto
{
    public Guid PinId { get; set; }
    public Guid MapId { get; set; }
    public Guid LayerId { get; set; }
    public string? Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public LinkedArticleSummaryDto? LinkedArticle { get; set; }
}

/// <summary>
/// Request DTO for updating pin position.
/// Coordinates are normalized to the 0..1 range.
/// </summary>
[ExcludeFromCodeCoverage]
public class MapPinPositionUpdateDto
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// Request DTO for toggling map layer visibility.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateLayerVisibilityRequest
{
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request DTO for reordering map layers.
/// </summary>
[ExcludeFromCodeCoverage]
public class ReorderLayersRequest
{
    public IList<Guid> LayerIds { get; set; } = new List<Guid>();
}

/// <summary>
/// Request DTO for renaming a map layer.
/// </summary>
[ExcludeFromCodeCoverage]
public class RenameLayerRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for assigning or clearing a map layer parent.
/// </summary>
[ExcludeFromCodeCoverage]
public class SetLayerParentRequest
{
    public Guid? ParentLayerId { get; set; }
}

/// <summary>
/// Request DTO to initiate a basemap upload. Returns a SAS URL for direct
/// client-to-blob upload.
/// </summary>
[ExcludeFromCodeCoverage]
public class RequestBasemapUploadDto
{
    /// <summary>
    /// Original filename of the basemap image.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type. Must be image/png, image/jpeg, or image/webp.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO containing the SAS upload URL for the basemap.
/// </summary>
[ExcludeFromCodeCoverage]
public class RequestBasemapUploadResponseDto
{
    /// <summary>
    /// SAS URL valid for 15 minutes with write-only permissions.
    /// The client should PUT the file directly to this URL.
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO containing the SAS read URL for a map basemap.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetBasemapReadUrlResponseDto
{
    /// <summary>
    /// SAS URL valid for short-lived read-only access to the basemap blob.
    /// </summary>
    public string ReadUrl { get; set; } = string.Empty;
}
