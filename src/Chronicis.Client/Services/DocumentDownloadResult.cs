namespace Chronicis.Client.Services;

/// <summary>
/// Result containing a SAS URL for downloading a document directly from blob storage.
/// </summary>
public sealed record DocumentDownloadResult(
    string DownloadUrl,
    string FileName,
    string ContentType,
    long FileSizeBytes);
