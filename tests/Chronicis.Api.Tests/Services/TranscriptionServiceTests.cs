using System.Net;
using Chronicis.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class TranscriptionServiceTests
{
    private static readonly byte[] SampleImageBytes = [0x89, 0x50, 0x4E, 0x47];

    private static (TranscriptionService Sut, HttpClient Client) CreateSut(HttpMessageHandler handler, TimeSpan? timeout = null)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://ocr.test/") };
        var sut = new TranscriptionService(client, NullLogger<TranscriptionService>.Instance,
            timeout ?? TranscriptionService.DefaultTimeout);
        return (sut, client);
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsSuccess_WhenApiReturnsText()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, """{"text":"Hello world"}""");
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://ocr.test/") };
        using (client)
        {
            // Use the public constructor (default 60s timeout) to verify that path
            var sut = new TranscriptionService(client, NullLogger<TranscriptionService>.Instance);

            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.True(result.Success);
            Assert.Equal("Hello world", result.Text);
            Assert.Null(result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenApiReturnsEmptyText()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, """{"text":""}""");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription produced no text.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenApiReturnsWhitespaceText()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, """{"text":"   "}""");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription produced no text.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenApiReturnsNullText()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, """{"text":null}""");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription produced no text.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenApiReturnsErrorStatus()
    {
        using var handler = new FakeHandler(HttpStatusCode.InternalServerError, "something broke");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription service returned status 500.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenApiReturnsBadRequest()
    {
        using var handler = new FakeHandler(HttpStatusCode.BadRequest, "bad image");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription service returned status 400.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ThrowsOperationCanceled_WhenCallerCancels()
    {
        using var handler = new DelayHandler(TimeSpan.FromSeconds(120));
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.TranscribeImageAsync(SampleImageBytes, cts.Token));
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenHttpRequestFails()
    {
        using var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription service is unavailable.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsFailure_WhenResponseIsInvalidJson()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, "not json at all {{{");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Equal("Transcription service returned an invalid response.", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_TrimsText_WhenApiReturnsTextWithWhitespace()
    {
        using var handler = new FakeHandler(HttpStatusCode.OK, """{"text":"  trimmed text  "}""");
        var (sut, client) = CreateSut(handler);
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.True(result.Success);
            Assert.Equal("trimmed text", result.Text);
        }
    }

    [Fact]
    public async Task TranscribeImageAsync_ReturnsTimeout_WhenInternalTimeoutExpires()
    {
        using var handler = new DelayHandler(TimeSpan.FromSeconds(5));
        var (sut, client) = CreateSut(handler, timeout: TimeSpan.FromMilliseconds(50));
        using (client)
        {
            var result = await sut.TranscribeImageAsync(SampleImageBytes);

            Assert.False(result.Success);
            Assert.Contains("timed out", result.ErrorMessage);
        }
    }

    #region Test Helpers

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            });
        }
    }

    private sealed class DelayHandler : HttpMessageHandler
    {
        private readonly TimeSpan _delay;

        public DelayHandler(TimeSpan delay) => _delay = delay;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"text":"late"}""")
            };
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    #endregion
}
