using System.Net;
using System.Text.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class HandwrittenNoteApiServiceTests
{
    private readonly Guid _articleId = Guid.NewGuid();
    private readonly byte[] _imageBytes = [0x89, 0x50, 0x4E, 0x47];

    private HandwrittenNoteApiService CreateSut(HttpClient http) =>
        new(http, NullLogger<HandwrittenNoteApiService>.Instance);

    // --- SaveHandwrittenNoteAsync ---

    [Fact]
    public async Task SaveHandwrittenNoteAsync_Success_ReturnsDto()
    {
        var expected = new HandwrittenNoteSaveResultDto { DocumentId = Guid.NewGuid(), DownloadUrl = "https://blob/img.png" };
        var json = JsonSerializer.Serialize(expected);
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json);
        var sut = CreateSut(http);

        var result = await sut.SaveHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.NotNull(result);
        Assert.Equal(expected.DocumentId, result.DocumentId);
        Assert.Equal(expected.DownloadUrl, result.DownloadUrl);
    }

    [Fact]
    public async Task SaveHandwrittenNoteAsync_Failure_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(http);

        var result = await sut.SaveHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveHandwrittenNoteAsync_Exception_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        var result = await sut.SaveHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveHandwrittenNoteAsync_SendsCorrectUrl()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new HandwrittenNoteSaveResultDto()))
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        await sut.SaveHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Single(calls);
        Assert.Equal($"articles/{_articleId}/handwritten-note", calls[0]);
    }

    // --- TranscribeHandwrittenNoteAsync ---

    [Fact]
    public async Task TranscribeHandwrittenNoteAsync_Success_ReturnsDto()
    {
        var expected = new HandwrittenNoteTranscribeResultDto
        {
            DocumentId = Guid.NewGuid(),
            DownloadUrl = "https://blob/img.png",
            TranscribedText = "Hello world"
        };
        var json = JsonSerializer.Serialize(expected);
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json);
        var sut = CreateSut(http);

        var result = await sut.TranscribeHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.NotNull(result);
        Assert.Equal(expected.DocumentId, result.DocumentId);
        Assert.Equal(expected.DownloadUrl, result.DownloadUrl);
        Assert.Equal(expected.TranscribedText, result.TranscribedText);
    }

    [Fact]
    public async Task TranscribeHandwrittenNoteAsync_Failure_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest);
        var sut = CreateSut(http);

        var result = await sut.TranscribeHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Null(result);
    }

    [Fact]
    public async Task TranscribeHandwrittenNoteAsync_Exception_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        var result = await sut.TranscribeHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Null(result);
    }

    [Fact]
    public async Task TranscribeHandwrittenNoteAsync_SendsCorrectUrl()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new HandwrittenNoteTranscribeResultDto()))
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        await sut.TranscribeHandwrittenNoteAsync(_articleId, _imageBytes);

        Assert.Single(calls);
        Assert.Equal($"articles/{_articleId}/handwritten-note/transcribe", calls[0]);
    }

    // --- GetHandwrittenNoteUrlAsync ---

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_Success_ReturnsUrl()
    {
        var json = JsonSerializer.Serialize(new { DownloadUrl = "https://blob/img.png" });
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json);
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Equal("https://blob/img.png", result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_NotFound_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound);
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_NoContent_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.NoContent);
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_NonSuccessStatus_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_Exception_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("network fail"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_SendsCorrectUrl()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { DownloadUrl = "u" }))
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Single(calls);
        Assert.Equal($"articles/{_articleId}/handwritten-note", calls[0]);
    }

    [Fact]
    public async Task GetHandwrittenNoteUrlAsync_NullDto_ReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null");
        var sut = CreateSut(http);

        var result = await sut.GetHandwrittenNoteUrlAsync(_articleId);

        Assert.Null(result);
    }

    // --- DeleteHandwrittenNoteAsync ---

    [Fact]
    public async Task DeleteHandwrittenNoteAsync_Success_ReturnsTrue()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK);
        var sut = CreateSut(http);

        var result = await sut.DeleteHandwrittenNoteAsync(_articleId);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteHandwrittenNoteAsync_Failure_ReturnsFalse()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(http);

        var result = await sut.DeleteHandwrittenNoteAsync(_articleId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteHandwrittenNoteAsync_Exception_ReturnsFalse()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        var result = await sut.DeleteHandwrittenNoteAsync(_articleId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteHandwrittenNoteAsync_SendsCorrectUrl()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        await sut.DeleteHandwrittenNoteAsync(_articleId);

        Assert.Single(calls);
        Assert.Equal($"articles/{_articleId}/handwritten-note", calls[0]);
    }

    [Fact]
    public async Task DeleteHandwrittenNoteAsync_UsesDeleteMethod()
    {
        HttpMethod? capturedMethod = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            capturedMethod = req.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = CreateSut(http);

        await sut.DeleteHandwrittenNoteAsync(_articleId);

        Assert.Equal(HttpMethod.Delete, capturedMethod);
    }
}
