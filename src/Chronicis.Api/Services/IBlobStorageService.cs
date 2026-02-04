using System.IO;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing Azure Blob Storage operations for world documents.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Generate a SAS URL for uploading a file directly from the client.
    /// </summary>
    /// <param name="worldId">The world ID for scoping the blob path.</param>
    /// <param name="documentId">The document ID (pre-created).</param>
    /// <param name="fileName">The original filename.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <returns>SAS URL valid for 15 minutes with write-only permissions.</returns>
    Task<string> GenerateUploadSasUrlAsync(Guid worldId, Guid documentId, string fileName, string contentType);

    /// <summary>
    /// Verify that a blob exists and get its metadata.
    /// </summary>
    /// <param name="blobPath">The blob path to verify.</param>
    /// <returns>Metadata including size and content type, or null if not found.</returns>
    Task<BlobMetadata?> GetBlobMetadataAsync(string blobPath);

    /// <summary>
    /// Open a read-only stream for the blob content.
    /// </summary>
    /// <param name="blobPath">The blob path to read.</param>
    /// <returns>Stream for reading the blob content.</returns>
    Task<Stream> OpenReadAsync(string blobPath);

    /// <summary>
    /// Delete a blob from storage.
    /// </summary>
    /// <param name="blobPath">The blob path to delete.</param>
    Task DeleteBlobAsync(string blobPath);

    /// <summary>
    /// Generate a SAS URL for downloading a file directly from the client.
    /// </summary>
    /// <param name="blobPath">The blob path to generate download URL for.</param>
    /// <returns>SAS URL valid for 15 minutes with read-only permissions.</returns>
    Task<string> GenerateDownloadSasUrlAsync(string blobPath);

    /// <summary>
    /// Build the blob path for a document.
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="fileName">The sanitized filename.</param>
    /// <returns>The blob path.</returns>
    string BuildBlobPath(Guid worldId, Guid documentId, string fileName);
}

/// <summary>
/// Metadata about a blob in storage.
/// </summary>
public class BlobMetadata
{
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
