using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ExternalLinkApiServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenSourceBlank()
    {
        var sut = new ExternalLinkApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<ExternalLinkApiService>.Instance);

        var result = await sut.GetSuggestionsAsync(Guid.NewGuid(), "", "q", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_OnCancelledOrException()
    {
        var cancelHandler = new TestHttpMessageHandler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        var cancel = new ExternalLinkApiService(new HttpClient(cancelHandler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ExternalLinkApiService>.Instance);
        var ex = new ExternalLinkApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ExternalLinkApiService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Empty(await cancel.GetSuggestionsAsync(null, "srd", "q", cts.Token));
        Assert.Empty(await ex.GetSuggestionsAsync(null, "srd", "q", CancellationToken.None));
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenBodyIsNull()
    {
        var sut = new ExternalLinkApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"),
            NullLogger<ExternalLinkApiService>.Instance);

        var result = await sut.GetSuggestionsAsync(Guid.NewGuid(), "srd", "q", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetContentAsync_ReturnsNull_ForInvalidInputsAndErrors()
    {
        var sut = new ExternalLinkApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<ExternalLinkApiService>.Instance);
        var ex = new ExternalLinkApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ExternalLinkApiService>.Instance);

        Assert.Null(await sut.GetContentAsync("", "id", CancellationToken.None));
        Assert.Null(await sut.GetContentAsync("srd", "", CancellationToken.None));
        Assert.Null(await ex.GetContentAsync("srd", "id", CancellationToken.None));
    }

    [Fact]
    public async Task GetContentAsync_ReturnsNull_OnCancellation()
    {
        var cancelHandler = new TestHttpMessageHandler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        var sut = new ExternalLinkApiService(new HttpClient(cancelHandler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ExternalLinkApiService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sut.GetContentAsync("srd", "id", cts.Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSuggestionsAsync_BuildsUrl_WithNullQueryAndNoWorldId()
    {
        string? requestedPath = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            requestedPath = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });
        var sut = new ExternalLinkApiService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<ExternalLinkApiService>.Instance);

        var result = await sut.GetSuggestionsAsync(null, "srd", null!, CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal("/external-links/suggestions?source=srd&query=", requestedPath);
    }

    [Fact]
    public async Task SuccessPaths_ReturnExpectedObjects()
    {
        var sut = new ExternalLinkApiService(
            TestHttpMessageHandler.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"source\":\"srd\",\"id\":\"/api/1\",\"title\":\"T\"}]")
            }),
            NullLogger<ExternalLinkApiService>.Instance);

        var suggestions = await sut.GetSuggestionsAsync(Guid.NewGuid(), "srd", "q", CancellationToken.None);

        Assert.Single(suggestions);

        var contentService = new ExternalLinkApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"source\":\"srd\",\"id\":\"/api/1\",\"title\":\"T\"}"),
            NullLogger<ExternalLinkApiService>.Instance);

        var content = await contentService.GetContentAsync("srd", "/api/1", CancellationToken.None);
        Assert.NotNull(content);
    }
}

