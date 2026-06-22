// Feature: handwritten-session-notes, Property 5: Transcription Stores Result in Body
using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using FsCheck;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Property 5: Transcription Stores Result in Body
/// For any non-empty transcription result string, calling TranscribeAsync stores
/// that string in the Article.Body field in the database.
///
/// Validates: Requirements 4.3
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteServiceTranscriptionPropertyTests
{
    [Fact]
    public void Transcription_Result_Is_Stored_In_Article_Body()
    {
        // **Validates: Requirements 4.3**
        Prop.ForAll(Arb.From<NonEmptyString>(), transcriptionText =>
        {
            var text = transcriptionText.Get;

            using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();

            var worldId = Guid.NewGuid();
            var articleId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            db.Worlds.Add(new World { Id = worldId, Name = "TestWorld", OwnerId = userId });
            db.Articles.Add(new Article
            {
                Id = articleId,
                WorldId = worldId,
                Title = "Test",
                Slug = "test",
                Type = ArticleType.SessionNote,
                CreatedBy = userId
            });
            db.SaveChanges();

            var blobStorage = Substitute.For<IBlobStorageService>();
            blobStorage.BuildBlobPath(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
                .Returns("fake/path.png");
            blobStorage.UploadBlobAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>())
                .Returns(Task.CompletedTask);
            blobStorage.GenerateDownloadSasUrlAsync(Arg.Any<string>())
                .Returns("https://fake.url/sas");

            var transcriptionService = Substitute.For<ITranscriptionService>();
            transcriptionService.TranscribeImageAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                .Returns(new TranscriptionResultDto { Success = true, Text = text });

            var logger = Substitute.For<ILogger<HandwrittenNoteService>>();

            var sut = new HandwrittenNoteService(db, blobStorage, transcriptionService, logger);

            sut.TranscribeAsync(articleId, userId, new byte[] { 0x89, 0x50 }).GetAwaiter().GetResult();

            var article = db.Articles.First(a => a.Id == articleId);
            return article.Body == text;
        }).QuickCheckThrowOnFailure();
    }
}
