using System.Net;
using System.Net.Http.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Sessions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class SessionApiServiceTests
{
    [Fact]
    public async Task WrapperMethods_HitExpectedEndpoints()
    {
        var requests = new List<string>();
        var sessionId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            requests.Add($"{req.Method} {req.RequestUri!.PathAndQuery}");
            var body = req.RequestUri!.AbsolutePath.Contains("/ai-summary/generate", StringComparison.Ordinal)
                ? "{\"success\":true,\"summary\":\"ok\"}"
                : req.RequestUri!.AbsolutePath.Contains("/sessions/", StringComparison.Ordinal) && req.Method == HttpMethod.Get
                    ? $"{{\"id\":\"{sessionId}\",\"arcId\":\"{arcId}\",\"name\":\"S\"}}"
                    : "[]";

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == $"/arcs/{arcId}/sessions")
            {
                body = $"{{\"id\":\"{sessionId}\",\"arcId\":\"{arcId}\",\"name\":\"Created\"}}";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        });
        var sut = new SessionApiService(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<SessionApiService>.Instance);

        _ = await sut.GetSessionsByArcAsync(arcId);
        _ = await sut.CreateSessionAsync(arcId, new SessionCreateDto { Name = "New" });
        _ = await sut.GetSessionAsync(sessionId);
        _ = await sut.DeleteSessionAsync(sessionId);
        _ = await sut.ClearAiSummaryAsync(sessionId);

        Assert.Contains($"GET /arcs/{arcId}/sessions", requests);
        Assert.Contains($"POST /arcs/{arcId}/sessions", requests);
        Assert.Contains($"GET /sessions/{sessionId}", requests);
        Assert.Contains($"DELETE /sessions/{sessionId}", requests);
        Assert.Contains($"DELETE /sessions/{sessionId}/ai-summary", requests);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_ReturnsEntity_OnSuccess()
    {
        var id = Guid.NewGuid();
        SessionUpdateDto? sent = null;
        var handler = new TestHttpMessageHandler(async (req, _) =>
        {
            Assert.Equal(HttpMethod.Patch, req.Method);
            Assert.Equal($"/sessions/{id}", req.RequestUri!.AbsolutePath);
            sent = await req.Content!.ReadFromJsonAsync<SessionUpdateDto>(CancellationToken.None);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"id\":\"{id}\",\"arcId\":\"{Guid.NewGuid()}\",\"name\":\"Updated\"}}")
            };
        });
        var sut = new SessionApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<SessionApiService>.Instance);

        var result = await sut.UpdateSessionNotesAsync(id, new SessionUpdateDto { Name = "Updated", PublicNotes = "<p>x</p>" });

        Assert.NotNull(result);
        Assert.Equal("Updated", result!.Name);
        Assert.NotNull(sent);
        Assert.Equal("Updated", sent!.Name);
        Assert.Equal("<p>x</p>", sent.PublicNotes);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_ReturnsNull_OnNonSuccess()
    {
        var sut = new SessionApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "bad"),
            NullLogger<SessionApiService>.Instance);

        var result = await sut.UpdateSessionNotesAsync(Guid.NewGuid(), new SessionUpdateDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_ReturnsNull_OnException()
    {
        var sut = new SessionApiService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<SessionApiService>.Instance);

        var result = await sut.UpdateSessionNotesAsync(Guid.NewGuid(), new SessionUpdateDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAiSummaryAsync_ReturnsEntity_OnSuccess()
    {
        var id = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"/sessions/{id}/ai-summary/generate", req.RequestUri!.AbsolutePath);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"summary\":\"hello\"}")
            });
        });
        var sut = new SessionApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<SessionApiService>.Instance);

        var result = await sut.GenerateAiSummaryAsync(id);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("hello", result.Summary);
    }

    [Fact]
    public async Task GenerateAiSummaryAsync_ReturnsNull_OnNonSuccessOrException()
    {
        var bad = new SessionApiService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "bad"),
            NullLogger<SessionApiService>.Instance);
        var ex = new SessionApiService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<SessionApiService>.Instance);

        Assert.Null(await bad.GenerateAiSummaryAsync(Guid.NewGuid()));
        Assert.Null(await ex.GenerateAiSummaryAsync(Guid.NewGuid()));
    }
}
