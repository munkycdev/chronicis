using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class HandwrittenNoteServiceTests
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<HandwrittenNoteService> _logger;

    public HandwrittenNoteServiceTests()
    {
        _blobStorage = Substitute.For<IBlobStorageService>();
        _transcriptionService = Substitute.For<ITranscriptionService>();
        _logger = Substitute.For<ILogger<HandwrittenNoteService>>();
    }

    private static ChronicisDbContext CreateDb() => RemainingApiBranchCoverageTestHelpers.CreateDbContext();

    private HandwrittenNoteService CreateSut(ChronicisDbContext db) =>
        new(db, _blobStorage, _transcriptionService, _logger);

    private static Article CreateArticle(ChronicisDbContext db, Guid? handwrittenNoteImageId = null)
    {
        var worldId = Guid.NewGuid();
        var world = new World { Id = worldId, Name = "TestWorld", OwnerId = Guid.NewGuid() };
        db.Worlds.Add(world);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Title = "Session 1",
            Slug = "session-1",
            CreatedBy = Guid.NewGuid(),
            HandwrittenNoteImageId = handwrittenNoteImageId
        };
        db.Articles.Add(article);
        db.SaveChanges();
        return article;
    }

    // ── SaveAsync ──────────────────────────────────

    [Fact]
    public async Task SaveAsync_CreatesDocumentAndLinksToArticle()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);
        var userId = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3 };

        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("worlds/x/documents/y/handwritten-note.png");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://blob.test/sas");

        var result = await sut.SaveAsync(article.Id, userId, imageBytes);

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal("https://blob.test/sas", result.DownloadUrl);

        var updatedArticle = await db.Articles.FirstAsync(a => a.Id == article.Id);
        Assert.Equal(result.DocumentId, updatedArticle.HandwrittenNoteImageId);

        var doc = await db.WorldDocuments.FirstAsync(d => d.Id == result.DocumentId);
        Assert.Equal("image/png", doc.ContentType);
        Assert.Equal(3, doc.FileSizeBytes);

        await _blobStorage.Received(1).UploadBlobAsync(
            Arg.Any<string>(), imageBytes, "image/png");
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingNote()
    {
        using var db = CreateDb();
        var oldDocId = Guid.NewGuid();
        var article = CreateArticle(db, oldDocId);

        var oldDoc = new WorldDocument
        {
            Id = oldDocId,
            WorldId = article.WorldId!.Value,
            ArticleId = article.Id,
            FileName = "handwritten-note.png",
            Title = "handwritten-note.png",
            BlobPath = "worlds/x/documents/old/handwritten-note.png",
            ContentType = "image/png",
            FileSizeBytes = 100,
            UploadedById = Guid.NewGuid()
        };
        db.WorldDocuments.Add(oldDoc);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("worlds/x/documents/new/handwritten-note.png");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://blob.test/new-sas");

        var result = await sut.SaveAsync(article.Id, Guid.NewGuid(), new byte[] { 4, 5 });

        Assert.NotEqual(oldDocId, result.DocumentId);
        Assert.False(await db.WorldDocuments.AnyAsync(d => d.Id == oldDocId));
        await _blobStorage.Received(1).DeleteBlobAsync("worlds/x/documents/old/handwritten-note.png");
    }

    [Fact]
    public async Task SaveAsync_ThrowsWhenArticleNotFound()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), new byte[] { 1 }));
    }

    // ── TranscribeAsync ──────────────────────────────────

    [Fact]
    public async Task TranscribeAsync_SavesAndTranscribes()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);
        var imageBytes = new byte[] { 10, 20 };

        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("path/to/blob");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://blob.test/dl");
        _transcriptionService.TranscribeImageAsync(imageBytes, Arg.Any<CancellationToken>())
            .Returns(new TranscriptionResultDto { Success = true, Text = "Hello world" });

        var result = await sut.TranscribeAsync(article.Id, Guid.NewGuid(), imageBytes);

        Assert.Equal("Hello world", result.TranscribedText);
        Assert.Equal("https://blob.test/dl", result.DownloadUrl);

        var updatedArticle = await db.Articles.FirstAsync(a => a.Id == article.Id);
        Assert.Equal("Hello world", updatedArticle.Body);
    }

    [Fact]
    public async Task TranscribeAsync_ThrowsOnTranscriptionFailure()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);

        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("path");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://url");
        _transcriptionService.TranscribeImageAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptionResultDto { Success = false, ErrorMessage = "OCR failed" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.TranscribeAsync(article.Id, Guid.NewGuid(), new byte[] { 1 }));
        Assert.Equal("OCR failed", ex.Message);
    }

    [Fact]
    public async Task TranscribeAsync_ThrowsGenericMessageWhenErrorMessageNull()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);

        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("path");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://url");
        _transcriptionService.TranscribeImageAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptionResultDto { Success = false, ErrorMessage = null });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.TranscribeAsync(article.Id, Guid.NewGuid(), new byte[] { 1 }));
        Assert.Equal("Transcription failed.", ex.Message);
    }

    // ── GetImageDownloadUrlAsync ──────────────────────────────────

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsUrlWhenNoteExists()
    {
        using var db = CreateDb();
        var docId = Guid.NewGuid();
        var article = CreateArticle(db, docId);

        db.WorldDocuments.Add(new WorldDocument
        {
            Id = docId,
            WorldId = article.WorldId!.Value,
            ArticleId = article.Id,
            FileName = "handwritten-note.png",
            Title = "handwritten-note.png",
            BlobPath = "worlds/x/documents/d/handwritten-note.png",
            ContentType = "image/png",
            FileSizeBytes = 50,
            UploadedById = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        _blobStorage.GenerateDownloadSasUrlAsync("worlds/x/documents/d/handwritten-note.png")
            .Returns("https://blob.test/download");

        var sut = CreateSut(db);
        var url = await sut.GetImageDownloadUrlAsync(article.Id, Guid.NewGuid());

        Assert.Equal("https://blob.test/download", url);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsNullWhenNoNote()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);

        var url = await sut.GetImageDownloadUrlAsync(article.Id, Guid.NewGuid());
        Assert.Null(url);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsNullWhenArticleNotFound()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        var url = await sut.GetImageDownloadUrlAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Null(url);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsNullWhenDocumentMissing()
    {
        using var db = CreateDb();
        var article = CreateArticle(db, Guid.NewGuid()); // points to nonexistent doc
        var sut = CreateSut(db);

        var url = await sut.GetImageDownloadUrlAsync(article.Id, Guid.NewGuid());
        Assert.Null(url);
    }

    // ── DeleteAsync ──────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesNoteAndNullsFK()
    {
        using var db = CreateDb();
        var docId = Guid.NewGuid();
        var article = CreateArticle(db, docId);

        db.WorldDocuments.Add(new WorldDocument
        {
            Id = docId,
            WorldId = article.WorldId!.Value,
            ArticleId = article.Id,
            FileName = "handwritten-note.png",
            Title = "handwritten-note.png",
            BlobPath = "worlds/x/documents/del/handwritten-note.png",
            ContentType = "image/png",
            FileSizeBytes = 10,
            UploadedById = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.DeleteAsync(article.Id, Guid.NewGuid());

        var updatedArticle = await db.Articles.FirstAsync(a => a.Id == article.Id);
        Assert.Null(updatedArticle.HandwrittenNoteImageId);
        Assert.False(await db.WorldDocuments.AnyAsync(d => d.Id == docId));
        await _blobStorage.Received(1).DeleteBlobAsync("worlds/x/documents/del/handwritten-note.png");
    }

    [Fact]
    public async Task DeleteAsync_NoOpWhenNoHandwrittenNote()
    {
        using var db = CreateDb();
        var article = CreateArticle(db);
        var sut = CreateSut(db);

        await sut.DeleteAsync(article.Id, Guid.NewGuid()); // should not throw
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenArticleNotFound()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── Blob deletion failure handling ──────────────────────────────────

    [Fact]
    public async Task SaveAsync_Replace_ContinuesWhenBlobDeletionFails()
    {
        using var db = CreateDb();
        var oldDocId = Guid.NewGuid();
        var article = CreateArticle(db, oldDocId);

        db.WorldDocuments.Add(new WorldDocument
        {
            Id = oldDocId,
            WorldId = article.WorldId!.Value,
            ArticleId = article.Id,
            FileName = "handwritten-note.png",
            Title = "handwritten-note.png",
            BlobPath = "old/path",
            ContentType = "image/png",
            FileSizeBytes = 5,
            UploadedById = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        _blobStorage.DeleteBlobAsync("old/path").Throws(new Exception("blob gone"));
        _blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("new/path");
        _blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns("https://url");

        var sut = CreateSut(db);
        var result = await sut.SaveAsync(article.Id, Guid.NewGuid(), new byte[] { 9 });

        // Old doc removed from DB despite blob failure
        Assert.False(await db.WorldDocuments.AnyAsync(d => d.Id == oldDocId));
        Assert.NotEqual(Guid.Empty, result.DocumentId);
    }

    [Fact]
    public async Task DeleteAsync_ContinuesWhenBlobDeletionFails()
    {
        using var db = CreateDb();
        var docId = Guid.NewGuid();
        var article = CreateArticle(db, docId);

        db.WorldDocuments.Add(new WorldDocument
        {
            Id = docId,
            WorldId = article.WorldId!.Value,
            ArticleId = article.Id,
            FileName = "handwritten-note.png",
            Title = "handwritten-note.png",
            BlobPath = "fail/path",
            ContentType = "image/png",
            FileSizeBytes = 7,
            UploadedById = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        _blobStorage.DeleteBlobAsync("fail/path").Throws(new Exception("storage down"));

        var sut = CreateSut(db);
        await sut.DeleteAsync(article.Id, Guid.NewGuid());

        var updatedArticle = await db.Articles.FirstAsync(a => a.Id == article.Id);
        Assert.Null(updatedArticle.HandwrittenNoteImageId);
        Assert.False(await db.WorldDocuments.AnyAsync(d => d.Id == docId));
    }

    [Fact]
    public async Task DeleteAsync_NoOpWhenDocumentRecordMissing()
    {
        using var db = CreateDb();
        // Article points to a doc ID that doesn't exist in WorldDocuments
        var article = CreateArticle(db, Guid.NewGuid());
        var sut = CreateSut(db);

        // Should not throw, just null out the FK
        await sut.DeleteAsync(article.Id, Guid.NewGuid());

        var updatedArticle = await db.Articles.FirstAsync(a => a.Id == article.Id);
        Assert.Null(updatedArticle.HandwrittenNoteImageId);
    }
}
