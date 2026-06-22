// Feature: handwritten-session-notes, Property 8: Replace Overwrites Old Handwritten Note
using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using FsCheck;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Property 8: Replace Overwrites Old Handwritten Note
/// Generate articles with existing handwritten note, save new; verify old deleted and new linked.
///
/// Validates: Requirements 8.6
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteServiceReplacePropertyTests
{
    [Fact]
    public void Replace_Deletes_Old_Document_And_Links_New()
    {
        // **Validates: Requirements 8.6**
        var arb = Gen.ArrayOf(Gen.Choose(1, 255).Select(i => (byte)i))
               .Where(b => b.Length > 0)
               .ToArbitrary();

        Prop.ForAll(arb, newImageBytes =>
        {
            using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();

            // Set up world + article with existing handwritten note
            var worldId = Guid.NewGuid();
            var world = new World { Id = worldId, Name = "W", OwnerId = Guid.NewGuid() };
            db.Worlds.Add(world);

            var oldDocId = Guid.NewGuid();
            var article = new Article
            {
                Id = Guid.NewGuid(),
                WorldId = worldId,
                Title = "S",
                Slug = "s",
                CreatedBy = Guid.NewGuid(),
                HandwrittenNoteImageId = oldDocId
            };
            db.Articles.Add(article);

            var oldBlobPath = $"worlds/{worldId}/documents/{oldDocId}/handwritten-note.png";
            var oldDoc = new WorldDocument
            {
                Id = oldDocId,
                WorldId = worldId,
                ArticleId = article.Id,
                FileName = "handwritten-note.png",
                Title = "handwritten-note.png",
                BlobPath = oldBlobPath,
                ContentType = "image/png",
                FileSizeBytes = 100,
                UploadedById = Guid.NewGuid()
            };
            db.WorldDocuments.Add(oldDoc);
            db.SaveChanges();

            // Mock dependencies
            var blobStorage = Substitute.For<IBlobStorageService>();
            var transcriptionService = Substitute.For<ITranscriptionService>();
            var logger = Substitute.For<ILogger<HandwrittenNoteService>>();

            blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
                .Returns("new/blob/path");
            blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
                .Returns(Task.FromResult("https://new-url"));
            blobStorage.DeleteBlobAsync(Arg.Any<string>())
                .Returns(Task.CompletedTask);
            blobStorage.UploadBlobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>())
                .Returns(Task.CompletedTask);

            var sut = new HandwrittenNoteService(db, blobStorage, transcriptionService, logger);

            // Act
            var result = sut.SaveAsync(article.Id, Guid.NewGuid(), newImageBytes).GetAwaiter().GetResult();

            // Assert: old doc gone
            var oldExists = db.WorldDocuments.Any(d => d.Id == oldDocId);
            if (oldExists) return false;

            // Assert: DeleteBlobAsync called with old path
            blobStorage.Received(1).DeleteBlobAsync(oldBlobPath);

            // Assert: new doc exists with correct content type
            var newDoc = db.WorldDocuments.FirstOrDefault(d => d.Id == result.DocumentId);
            if (newDoc == null) return false;
            if (newDoc.ContentType != "image/png") return false;

            // Assert: article now references new doc
            var updatedArticle = db.Articles.First(a => a.Id == article.Id);
            return updatedArticle.HandwrittenNoteImageId == result.DocumentId
                && result.DocumentId != oldDocId;
        }).QuickCheckThrowOnFailure();
    }
}
