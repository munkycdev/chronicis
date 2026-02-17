using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing world documents (file uploads to blob storage).
/// </summary>
public interface IWorldDocumentService
{
    /// <summary>
    /// Validate file upload request and generate SAS URL for client upload.
    /// Creates a pending WorldDocument record.
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="userId">The user requesting the upload.</param>
    /// <param name="request">Upload request details.</param>
    /// <returns>Upload response with SAS URL and document ID.</returns>
    Task<WorldDocumentUploadResponseDto> RequestUploadAsync(
        Guid worldId,
        Guid userId,
        WorldDocumentUploadRequestDto request);

    /// <summary>
    /// Confirm that a file upload completed successfully.
    /// Verifies blob exists and updates the WorldDocument record.
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="documentId">The document ID from the upload request.</param>
    /// <param name="userId">The user confirming the upload.</param>
    /// <returns>The completed document DTO.</returns>
    Task<WorldDocumentDto> ConfirmUploadAsync(Guid worldId, Guid documentId, Guid userId);

    /// <summary>
    /// Get all documents for a world.
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <returns>List of documents sorted by upload date descending.</returns>
    Task<List<WorldDocumentDto>> GetWorldDocumentsAsync(Guid worldId, Guid userId);

    /// <summary>
    /// Get a download URL for the document content (generates SAS URL).
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <returns>Document download URL and metadata.</returns>
    Task<DocumentContentResult> GetDocumentContentAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Update document metadata (title, description).
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <param name="update">Updated metadata.</param>
    /// <returns>Updated document DTO.</returns>
    Task<WorldDocumentDto> UpdateDocumentAsync(
        Guid worldId,
        Guid documentId,
        Guid userId,
        WorldDocumentUpdateDto update);

    /// <summary>
    /// Delete a document (removes from database and blob storage).
    /// </summary>
    /// <param name="worldId">The world ID.</param>
    /// <param name="documentId">The document ID.</param>
    /// <param name="userId">The requesting user ID.</param>
    Task DeleteDocumentAsync(Guid worldId, Guid documentId, Guid userId);

    /// <summary>
    /// Delete all images/documents associated with an article.
    /// Used during article deletion to clean up orphaned blobs.
    /// </summary>
    /// <param name="articleId">The article being deleted.</param>
    Task DeleteArticleImagesAsync(Guid articleId);
}
