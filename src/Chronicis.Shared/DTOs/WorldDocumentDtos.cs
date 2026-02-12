namespace Chronicis.Shared.DTOs;

/// <summary>
/// Response DTO for WorldDocument.
/// </summary>
public class WorldDocumentDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public Guid? ArticleId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid UploadedById { get; set; }
}

/// <summary>
/// Request DTO to initiate a document upload.
/// Returns a SAS URL for direct client-to-blob upload.
/// </summary>
public class WorldDocumentUploadRequestDto
{
    /// <summary>
    /// Original filename to upload.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type (MIME type) of the file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes (for validation).
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional article ID for inline image uploads.
    /// </summary>
    public Guid? ArticleId { get; set; }
}

/// <summary>
/// Response DTO containing SAS URL for client upload.
/// </summary>
public class WorldDocumentUploadResponseDto
{
    /// <summary>
    /// The document ID (pre-created, pending upload).
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// SAS URL for client to upload file directly to blob storage.
    /// Valid for 15 minutes.
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// The title that will be used (may have been auto-renamed for conflicts).
    /// </summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO to confirm upload completion.
/// </summary>
public class WorldDocumentConfirmUploadDto
{
    /// <summary>
    /// Document ID from the upload response.
    /// </summary>
    public Guid DocumentId { get; set; }
}

/// <summary>
/// Request DTO to update document metadata.
/// </summary>
public class WorldDocumentUpdateDto
{
    /// <summary>
    /// Updated title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Updated description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Response DTO containing download URL for document.
/// </summary>
public class WorldDocumentDownloadDto
{
    /// <summary>
    /// Temporary SAS URL for downloading the document (valid for 15 minutes).
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Original filename of the document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the document.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }
}

