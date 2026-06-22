using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Controllers;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class HandwrittenNoteControllerTests
{
    private readonly IHandwrittenNoteService _noteService;
    private readonly ICurrentUserService _currentUserService;
    private readonly HandwrittenNoteController _sut;
    private readonly User _testUser;
    private readonly Guid _articleId;

    public HandwrittenNoteControllerTests()
    {
        _noteService = Substitute.For<IHandwrittenNoteService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _sut = new HandwrittenNoteController(
            _noteService,
            _currentUserService,
            NullLogger<HandwrittenNoteController>.Instance);

        _testUser = new User { Id = Guid.NewGuid(), Auth0UserId = "auth0|123", Email = "test@test.com" };
        _articleId = Guid.NewGuid();
        _currentUserService.GetRequiredUserAsync().Returns(_testUser);
    }

    private static HandwrittenNoteUploadRequest Req(byte[]? imageBytes) =>
        new() { ImageBytes = imageBytes ?? [] };

    // ────────────────────────────────────────────────────────────────
    //  POST / — SaveHandwrittenNote
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveHandwrittenNote_ReturnsOk_WhenSuccessful()
    {
        var imageBytes = new byte[] { 1, 2, 3 };
        var expected = new HandwrittenNoteSaveResultDto
        {
            DocumentId = Guid.NewGuid(),
            DownloadUrl = "https://blob.test/image.png"
        };
        _noteService.SaveAsync(_articleId, _testUser.Id, imageBytes).Returns(expected);

        var result = await _sut.SaveHandwrittenNote(_articleId, Req(imageBytes));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task SaveHandwrittenNote_ReturnsBadRequest_WhenRequestNull()
    {
        var result = await _sut.SaveHandwrittenNote(_articleId, null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SaveHandwrittenNote_ReturnsBadRequest_WhenImageBytesEmpty()
    {
        var result = await _sut.SaveHandwrittenNote(_articleId, Req(Array.Empty<byte>()));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SaveHandwrittenNote_ReturnsNotFound_WhenInvalidOperationException()
    {
        var imageBytes = new byte[] { 1, 2, 3 };
        _noteService.SaveAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new InvalidOperationException("Article not found"));

        var result = await _sut.SaveHandwrittenNote(_articleId, Req(imageBytes));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task SaveHandwrittenNote_Returns403_WhenUnauthorizedAccessException()
    {
        var imageBytes = new byte[] { 1, 2, 3 };
        _noteService.SaveAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        var result = await _sut.SaveHandwrittenNote(_articleId, Req(imageBytes));

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task SaveHandwrittenNote_Returns500_WhenGenericException()
    {
        var imageBytes = new byte[] { 1, 2, 3 };
        _noteService.SaveAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new Exception("Something broke"));

        var result = await _sut.SaveHandwrittenNote(_articleId, Req(imageBytes));

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ────────────────────────────────────────────────────────────────
    //  POST /transcribe — TranscribeHandwrittenNote
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TranscribeHandwrittenNote_ReturnsOk_WhenSuccessful()
    {
        var imageBytes = new byte[] { 4, 5, 6 };
        var expected = new HandwrittenNoteTranscribeResultDto
        {
            DocumentId = Guid.NewGuid(),
            DownloadUrl = "https://blob.test/image.png",
            TranscribedText = "Hello world"
        };
        _noteService.TranscribeAsync(_articleId, _testUser.Id, imageBytes).Returns(expected);

        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(imageBytes));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_ReturnsBadRequest_WhenRequestNull()
    {
        var result = await _sut.TranscribeHandwrittenNote(_articleId, null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_ReturnsBadRequest_WhenImageBytesEmpty()
    {
        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(Array.Empty<byte>()));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_ReturnsNotFound_WhenInvalidOperationException()
    {
        var imageBytes = new byte[] { 4, 5, 6 };
        _noteService.TranscribeAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new InvalidOperationException("Article not found"));

        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(imageBytes));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_Returns403_WhenUnauthorizedAccessException()
    {
        var imageBytes = new byte[] { 4, 5, 6 };
        _noteService.TranscribeAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(imageBytes));

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_Returns500_WhenGenericException()
    {
        var imageBytes = new byte[] { 4, 5, 6 };
        _noteService.TranscribeAsync(_articleId, _testUser.Id, imageBytes)
            .ThrowsAsync(new Exception("Something broke"));

        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(imageBytes));

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task TranscribeHandwrittenNote_PassesConfirmOverwrite_DefaultFalse()
    {
        var imageBytes = new byte[] { 4, 5, 6 };
        var expected = new HandwrittenNoteTranscribeResultDto
        {
            DocumentId = Guid.NewGuid(),
            DownloadUrl = "https://blob.test/image.png",
            TranscribedText = "Transcribed"
        };
        _noteService.TranscribeAsync(_articleId, _testUser.Id, imageBytes).Returns(expected);

        var result = await _sut.TranscribeHandwrittenNote(_articleId, Req(imageBytes), confirmOverwrite: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    // ────────────────────────────────────────────────────────────────
    //  GET / — GetHandwrittenNoteUrl
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHandwrittenNoteUrl_ReturnsOk_WhenUrlExists()
    {
        var expectedUrl = "https://blob.test/download/image.png";
        _noteService.GetImageDownloadUrlAsync(_articleId, _testUser.Id).Returns(expectedUrl);

        var result = await _sut.GetHandwrittenNoteUrl(_articleId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrl_ReturnsNotFound_WhenUrlNull()
    {
        _noteService.GetImageDownloadUrlAsync(_articleId, _testUser.Id).Returns((string?)null);

        var result = await _sut.GetHandwrittenNoteUrl(_articleId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrl_ReturnsNotFound_WhenInvalidOperationException()
    {
        _noteService.GetImageDownloadUrlAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new InvalidOperationException("Article not found"));

        var result = await _sut.GetHandwrittenNoteUrl(_articleId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrl_Returns403_WhenUnauthorizedAccessException()
    {
        _noteService.GetImageDownloadUrlAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        var result = await _sut.GetHandwrittenNoteUrl(_articleId);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrl_Returns500_WhenGenericException()
    {
        _noteService.GetImageDownloadUrlAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new Exception("Something broke"));

        var result = await _sut.GetHandwrittenNoteUrl(_articleId);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ────────────────────────────────────────────────────────────────
    //  DELETE / — DeleteHandwrittenNote
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteHandwrittenNote_ReturnsNoContent_WhenSuccessful()
    {
        var result = await _sut.DeleteHandwrittenNote(_articleId);

        Assert.IsType<NoContentResult>(result);
        await _noteService.Received(1).DeleteAsync(_articleId, _testUser.Id);
    }

    [Fact]
    public async Task DeleteHandwrittenNote_ReturnsNotFound_WhenInvalidOperationException()
    {
        _noteService.DeleteAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new InvalidOperationException("Article not found"));

        var result = await _sut.DeleteHandwrittenNote(_articleId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteHandwrittenNote_Returns403_WhenUnauthorizedAccessException()
    {
        _noteService.DeleteAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        var result = await _sut.DeleteHandwrittenNote(_articleId);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteHandwrittenNote_Returns500_WhenGenericException()
    {
        _noteService.DeleteAsync(_articleId, _testUser.Id)
            .ThrowsAsync(new Exception("Something broke"));

        var result = await _sut.DeleteHandwrittenNote(_articleId);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
