namespace Chronicis.Api.Services;

/// <summary>
/// Result containing a SAS URL for downloading a document directly from blob storage.
/// </summary>
public sealed class DocumentContentResult
{
    public DocumentContentResult(string downloadUrl, string fileName, string contentType, long fileSizeBytes)
    {
        DownloadUrl = downloadUrl;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
    }

    /// <summary>
    /// Temporary SAS URL for downloading the document (valid for 15 minutes).
    /// </summary>
    public string DownloadUrl { get; }
    
    /// <summary>
    /// Original filename of the document.
    /// </summary>
    public string FileName { get; }
    
    /// <summary>
    /// MIME type of the document.
    /// </summary>
    public string ContentType { get; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; }
}
