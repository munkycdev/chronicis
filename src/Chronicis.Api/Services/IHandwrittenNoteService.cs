using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing handwritten note images on session note articles.
/// </summary>
public interface IHandwrittenNoteService
{
    /// <summary>
    /// Save a handwritten note image for a session note article.
    /// Replaces any existing handwritten note.
    /// </summary>
    Task<HandwrittenNoteSaveResultDto> SaveAsync(Guid articleId, Guid userId, byte[] imageBytes);

    /// <summary>
    /// Save a handwritten note image and transcribe it, storing the result in Article.Body.
    /// </summary>
    Task<HandwrittenNoteTranscribeResultDto> TranscribeAsync(Guid articleId, Guid userId, byte[] imageBytes);

    /// <summary>
    /// Get a download URL for the handwritten note image.
    /// </summary>
    Task<string?> GetImageDownloadUrlAsync(Guid articleId, Guid userId);

    /// <summary>
    /// Transcribe an already-saved handwritten note image, reading the blob server-side.
    /// </summary>
    Task<HandwrittenNoteTranscribeResultDto> TranscribeExistingAsync(Guid articleId, Guid userId);

    /// <summary>
    /// Delete the handwritten note image from a session note article.
    /// </summary>
    Task DeleteAsync(Guid articleId, Guid userId);
}
