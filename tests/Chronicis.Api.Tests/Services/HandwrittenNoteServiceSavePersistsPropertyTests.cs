// Feature: handwritten-session-notes, Property 4: Save Persists and Links Handwritten Note
using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using FsCheck;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Property 4: Save Persists and Links Handwritten Note
/// For any non-empty PNG byte stub and valid article, SaveAsync creates a WorldDocument
/// with ContentType "image/png" and links it to the article via HandwrittenNoteImageId.
///
/// **Validates: Requirements 3.3, 8.2**
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteServiceSavePersistsPropertyTests
{
    [Fact]
    public void Save_Creates_WorldDocument_With_Correct_ContentType_And_Links_Article()
    {
        // **Validates: Requirements 3.3, 8.2**
        Prop.ForAll(
            Arb.From<NonEmptyArray<byte>>(),
            Arb.From<Guid>(),
            Arb.From<Guid>(),
            (NonEmptyArray<byte> imageData, Guid articleId, Guid userId) =>
            {
                var imageBytes = imageData.Get;

                using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();

                // Set up world + article
                var worldId = Guid.NewGuid();
                db.Worlds.Add(new World { Id = worldId, Name = "W", OwnerId = Guid.NewGuid() });
                db.Articles.Add(new Article
                {
                    Id = articleId,
                    WorldId = worldId,
                    Title = "S",
                    Slug = "s",
                    CreatedBy = Guid.NewGuid()
                });
                db.SaveChanges();

                // Mocks
                var blobStorage = Substitute.For<IBlobStorageService>();
                blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns("blob/path");
                blobStorage.UploadBlobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>())
                    .Returns(Task.CompletedTask);
                blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
                    .Returns("https://dl");

                var transcription = Substitute.For<ITranscriptionService>();
                var logger = Substitute.For<ILogger<HandwrittenNoteService>>();

                var sut = new HandwrittenNoteService(db, blobStorage, transcription, logger);
                var result = sut.SaveAsync(articleId, userId, imageBytes).GetAwaiter().GetResult();

                // Verify WorldDocument
                var doc = db.WorldDocuments.FirstOrDefault(d => d.Id == result.DocumentId);
                if (doc == null) return false;
                if (doc.ContentType != "image/png") return false;
                if (doc.FileSizeBytes != imageBytes.Length) return false;

                // Verify article link
                var article = db.Articles.First(a => a.Id == articleId);
                return article.HandwrittenNoteImageId == doc.Id;
            }).QuickCheckThrowOnFailure();
    }
}
