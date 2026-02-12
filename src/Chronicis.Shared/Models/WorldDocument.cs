namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a document file stored in Azure Blob Storage associated with a World.
/// Examples: PDFs, images, Word docs, campaign materials, maps, etc.
/// </summary>
public class WorldDocument
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The world this document belongs to.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// Optional article this document is embedded in (for inline images).
    /// Null for standalone world documents.
    /// </summary>
    public Guid? ArticleId { get; set; }

    /// <summary>
    /// Original filename as uploaded by user.
    /// Max 255 characters.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the document (defaults to FileName, user can edit).
    /// Max 200 characters.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Path to the blob in Azure Storage.
    /// Format: worlds/{worldId}/documents/{documentId}/{sanitized-filename}
    /// Max 1024 characters.
    /// </summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "application/pdf", "image/png").
    /// Max 100 characters.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Optional description of what this document contains.
    /// Max 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who uploaded the document.
    /// </summary>
    public Guid UploadedById { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The world this document belongs to.
    /// </summary>
    public World World { get; set; } = null!;

    /// <summary>
    /// The article this document is embedded in (for inline images). Null for standalone documents.
    /// </summary>
    public Article? Article { get; set; }

    /// <summary>
    /// The user who uploaded this document.
    /// </summary>
    public User UploadedBy { get; set; } = null!;
}
