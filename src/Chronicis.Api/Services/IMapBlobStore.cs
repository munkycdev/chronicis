namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing Azure Blob Storage operations for map blobs.
/// </summary>
public interface IMapBlobStore
{
    /// <summary>
    /// Build the blob key for a map basemap.
    /// </summary>
    /// <param name="mapId">The map ID.</param>
    /// <param name="fileName">The original filename.</param>
    /// <returns>The blob key in the format maps/{mapId}/basemap/{sanitizedFilename}.</returns>
    string BuildBasemapBlobKey(Guid mapId, string fileName);

    /// <summary>
    /// Build the blob key for a map feature geometry payload.
    /// </summary>
    string BuildFeatureGeometryBlobKey(Guid mapId, Guid layerId, Guid featureId);

    /// <summary>
    /// Generate a SAS URL for uploading a basemap directly from the client.
    /// </summary>
    /// <param name="mapId">The map ID.</param>
    /// <param name="fileName">The original filename.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <returns>SAS URL valid for 15 minutes with write-only permissions.</returns>
    Task<string> GenerateUploadSasUrlAsync(Guid mapId, string fileName, string contentType);

    /// <summary>
    /// Generate a SAS URL for reading a basemap directly from the client.
    /// </summary>
    /// <param name="blobKey">The blob key to generate a read URL for.</param>
    /// <returns>SAS URL valid for 15 minutes with read-only permissions.</returns>
    Task<string> GenerateReadSasUrlAsync(string blobKey);

    /// <summary>
    /// Deletes all blobs under the map folder prefix (maps/{mapId}/...).
    /// </summary>
    /// <param name="mapId">The map ID whose blob folder should be deleted.</param>
    Task DeleteMapFolderAsync(Guid mapId);

    /// <summary>
    /// Saves compressed geometry JSON and returns the stored blob ETag.
    /// </summary>
    Task<string> SaveFeatureGeometryAsync(string blobKey, string geometryJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads and decompresses geometry JSON.
    /// </summary>
    Task<string?> LoadFeatureGeometryAsync(string blobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a map feature geometry blob if present.
    /// </summary>
    Task DeleteFeatureGeometryAsync(string blobKey, CancellationToken cancellationToken = default);
}
