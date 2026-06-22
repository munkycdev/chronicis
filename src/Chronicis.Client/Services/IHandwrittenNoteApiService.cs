using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Client service for handwritten note API operations.
/// </summary>
public interface IHandwrittenNoteApiService
{
    Task<HandwrittenNoteSaveResultDto?> SaveHandwrittenNoteAsync(Guid articleId, byte[] imageBytes);
    Task<HandwrittenNoteTranscribeResultDto?> TranscribeHandwrittenNoteAsync(Guid articleId, byte[] imageBytes);
    Task<HandwrittenNoteTranscribeResultDto?> TranscribeExistingAsync(Guid articleId);
    Task<string?> GetHandwrittenNoteUrlAsync(Guid articleId);
    Task<bool> DeleteHandwrittenNoteAsync(Guid articleId);
}
