// Feature: handwritten-session-notes, Property 7: Article Deletion Cascades to Handwritten Note Cleanup
using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using FsCheck;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Property 7: Article Deletion Cascades to Handwritten Note Cleanup
/// For any article, calling DeleteAsync removes the WorldDocument and blob when
/// a handwritten note is present, and is a no-op otherwise. In both cases,
/// HandwrittenNoteImageId is null afterward.
///
/// **Validates: Requirements 8.3**
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteServiceCascadeDeletePropertyTests
{
    [Fact]
    public void Delete_Cascades_WorldDocument_And_Blob_When_Present()
    {
        // **Validates: Requirements 8.3**
        Prop.ForAll(
            Arb.From<bool>(),
            Arb.From<Guid>(),
            (hasNote, articleGuid) =>
            {
                var worldGuid = Guid.NewGuid();
                var userGuid = Guid.NewGuid();
                // Skip empty guids to avoid EF key conflicts
                if (articleGuid == Guid.Empty)
                    return true;

                var blobStorage = Substitute.For<IBlobStorageService>();
                var transcription = Substitute.For<ITranscriptionService>();
                var logger = Substitute.For<ILogger<HandwrittenNoteService>>();

                using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();

                var world = new World { Id = worldGuid, Name = "W", OwnerId = userGuid };
                db.Worlds.Add(world);

                Guid? docId = null;
                var article = new Article
                {
                    Id = articleGuid,
                    WorldId = worldGuid,
                    Title = "T",
                    Slug = "t",
                    CreatedBy = userGuid
                };

                if (hasNote)
                {
                    docId = Guid.NewGuid();
                    article.HandwrittenNoteImageId = docId;
                    db.WorldDocuments.Add(new WorldDocument
                    {
                        Id = docId.Value,
                        WorldId = worldGuid,
                        ArticleId = articleGuid,
                        FileName = "handwritten-note.png",
                        Title = "handwritten-note.png",
                        BlobPath = $"worlds/{worldGuid}/documents/{docId}/handwritten-note.png",
                        ContentType = "image/png",
                        FileSizeBytes = 42,
                        UploadedById = userGuid
                    });
                }

                db.Articles.Add(article);
                db.SaveChanges();

                var sut = new HandwrittenNoteService(db, blobStorage, transcription, logger);
                sut.DeleteAsync(articleGuid, userGuid).GetAwaiter().GetResult();

                var updatedArticle = db.Articles.First(a => a.Id == articleGuid);

                // HandwrittenNoteImageId always null after delete
                if (updatedArticle.HandwrittenNoteImageId != null)
                    return false;

                if (hasNote)
                {
                    // WorldDocument removed
                    if (db.WorldDocuments.Any(d => d.Id == docId!.Value))
                        return false;

                    // Blob deletion called
                    blobStorage.Received(1).DeleteBlobAsync(
                        $"worlds/{worldGuid}/documents/{docId}/handwritten-note.png");
                }
                else
                {
                    // No blob calls when no note
                    blobStorage.DidNotReceive().DeleteBlobAsync(Arg.Any<string>());
                }

                return true;
            }).QuickCheckThrowOnFailure();
    }
}
