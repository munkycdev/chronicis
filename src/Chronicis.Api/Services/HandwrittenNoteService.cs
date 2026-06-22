using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Orchestrates handwritten note save, transcribe, download, and delete operations.
/// </summary>
public sealed class HandwrittenNoteService : IHandwrittenNoteService
{
    private readonly ChronicisDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<HandwrittenNoteService> _logger;

    private const string FileName = "handwritten-note.png";
    private const string ContentType = "image/png";

    public HandwrittenNoteService(
        ChronicisDbContext db,
        IBlobStorageService blobStorage,
        ITranscriptionService transcriptionService,
        ILogger<HandwrittenNoteService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HandwrittenNoteSaveResultDto> SaveAsync(Guid articleId, Guid userId, byte[] imageBytes)
    {
        _logger.LogTraceSanitized("Saving handwritten note for article {ArticleId} by user {UserId}", articleId, userId);

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId)
            ?? throw new InvalidOperationException("Article not found");

        // Replace existing handwritten note if present
        if (article.HandwrittenNoteImageId.HasValue)
        {
            await DeleteWorldDocumentAsync(article.HandwrittenNoteImageId.Value);
        }

        // Create new WorldDocument
        var document = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = article.WorldId!.Value,
            ArticleId = articleId,
            FileName = FileName,
            Title = FileName,
            ContentType = ContentType,
            FileSizeBytes = imageBytes.Length,
            UploadedAt = DateTime.UtcNow,
            UploadedById = userId
        };

        document.BlobPath = _blobStorage.BuildBlobPath(document.WorldId, document.Id, FileName);

        // Upload blob
        await _blobStorage.UploadBlobAsync(document.BlobPath, imageBytes, ContentType);

        // Persist record and link to article
        _db.WorldDocuments.Add(document);
        article.HandwrittenNoteImageId = document.Id;
        await _db.SaveChangesAsync();

        var downloadUrl = await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);

        _logger.LogTraceSanitized("Saved handwritten note {DocumentId} for article {ArticleId}", document.Id, articleId);

        return new HandwrittenNoteSaveResultDto
        {
            DocumentId = document.Id,
            DownloadUrl = downloadUrl
        };
    }

    /// <inheritdoc/>
    public async Task<HandwrittenNoteTranscribeResultDto> TranscribeAsync(Guid articleId, Guid userId, byte[] imageBytes)
    {
        _logger.LogTraceSanitized("Transcribing handwritten note for article {ArticleId}", articleId);

        var saveResult = await SaveAsync(articleId, userId, imageBytes);

        var transcriptionResult = await _transcriptionService.TranscribeImageAsync(imageBytes);

        if (!transcriptionResult.Success)
        {
            throw new InvalidOperationException(transcriptionResult.ErrorMessage ?? "Transcription failed.");
        }

        // Store transcribed text in article body
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId)
            ?? throw new InvalidOperationException("Article not found");

        article.Body = transcriptionResult.Text;
        await _db.SaveChangesAsync();

        _logger.LogTraceSanitized("Transcribed handwritten note for article {ArticleId}", articleId);

        return new HandwrittenNoteTranscribeResultDto
        {
            DocumentId = saveResult.DocumentId,
            DownloadUrl = saveResult.DownloadUrl,
            TranscribedText = transcriptionResult.Text
        };
    }

    /// <inheritdoc/>
    public async Task<string?> GetImageDownloadUrlAsync(Guid articleId, Guid userId)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (article?.HandwrittenNoteImageId == null)
            return null;

        var document = await _db.WorldDocuments.FirstOrDefaultAsync(d => d.Id == article.HandwrittenNoteImageId.Value);
        if (document == null)
            return null;

        return await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);
    }

    /// <inheritdoc/>
    public async Task<HandwrittenNoteTranscribeResultDto> TranscribeExistingAsync(Guid articleId, Guid userId)
    {
        _logger.LogTraceSanitized("Transcribing existing handwritten note for article {ArticleId} by user {UserId}", articleId, userId);

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId)
            ?? throw new InvalidOperationException("Article not found");

        if (!article.HandwrittenNoteImageId.HasValue)
            throw new InvalidOperationException("No handwritten note exists for this article");

        var document = await _db.WorldDocuments.FirstOrDefaultAsync(d => d.Id == article.HandwrittenNoteImageId.Value)
            ?? throw new InvalidOperationException("Handwritten note document not found");

        // Download blob bytes server-side
        await using var stream = await _blobStorage.OpenReadAsync(document.BlobPath);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var transcriptionResult = await _transcriptionService.TranscribeImageAsync(imageBytes, cts.Token);

        if (!transcriptionResult.Success || string.IsNullOrWhiteSpace(transcriptionResult.Text))
            throw new InvalidOperationException(transcriptionResult.ErrorMessage ?? "Transcription produced no text");

        article.Body = transcriptionResult.Text;
        await _db.SaveChangesAsync();

        var downloadUrl = await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);

        return new HandwrittenNoteTranscribeResultDto
        {
            DocumentId = document.Id,
            DownloadUrl = downloadUrl,
            TranscribedText = transcriptionResult.Text
        };
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid articleId, Guid userId)
    {
        _logger.LogTraceSanitized("Deleting handwritten note for article {ArticleId} by user {UserId}", articleId, userId);

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId)
            ?? throw new InvalidOperationException("Article not found");

        if (!article.HandwrittenNoteImageId.HasValue)
            return;

        await DeleteWorldDocumentAsync(article.HandwrittenNoteImageId.Value);
        article.HandwrittenNoteImageId = null;
        await _db.SaveChangesAsync();

        _logger.LogTraceSanitized("Deleted handwritten note for article {ArticleId}", articleId);
    }

    /// <summary>
    /// Delete a WorldDocument record and its blob. Blob deletion failure is logged and swallowed.
    /// </summary>
    private async Task DeleteWorldDocumentAsync(Guid documentId)
    {
        var document = await _db.WorldDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            return;

        try
        {
            await _blobStorage.DeleteBlobAsync(document.BlobPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarningSanitized(ex, "Failed to delete blob {BlobPath} for document {DocumentId}",
                document.BlobPath, document.Id);
        }

        _db.WorldDocuments.Remove(document);
    }
}
