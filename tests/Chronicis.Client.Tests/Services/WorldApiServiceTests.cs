using System.Net;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class WorldApiServiceTests
{
    [Fact]
    public async Task Methods_UseExpectedRoutes()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            var content = req.RequestUri!.ToString().Contains("documents/") && req.RequestUri.ToString().Contains("/content")
                ? "{\"downloadUrl\":\"u\",\"fileName\":\"f\",\"contentType\":\"text/plain\",\"fileSizeBytes\":1}"
                : "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new WorldApiService(http, NullLogger<WorldApiService>.Instance);
        var id = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await sut.GetWorldsAsync();
        await sut.GetWorldAsync(id);
        await sut.CreateWorldAsync(new WorldCreateDto { Name = "World" });
        await sut.UpdateWorldAsync(id, new WorldUpdateDto { Name = "World2" });
        await sut.GetWorldLinksAsync(id);
        await sut.CreateWorldLinkAsync(id, new WorldLinkCreateDto { Title = "t", Url = "u" });
        await sut.UpdateWorldLinkAsync(id, id2, new WorldLinkUpdateDto { Title = "t", Url = "u" });
        await sut.DeleteWorldLinkAsync(id, id2);
        await sut.CheckPublicSlugAsync(id, "slug");
        await sut.GetMembersAsync(id);
        await sut.UpdateMemberRoleAsync(id, id2, new WorldMemberUpdateDto { Role = Shared.Enums.WorldRole.Player });
        await sut.RemoveMemberAsync(id, id2);
        await sut.GetInvitationsAsync(id);
        await sut.CreateInvitationAsync(id, new WorldInvitationCreateDto { Role = Shared.Enums.WorldRole.Player });
        await sut.RevokeInvitationAsync(id, id2);
        await sut.JoinWorldAsync("code");
        await sut.RequestDocumentUploadAsync(id, new WorldDocumentUploadRequestDto { FileName = "a.txt", ContentType = "text/plain", FileSizeBytes = 1 });
        await sut.ConfirmDocumentUploadAsync(id, id2);
        await sut.GetWorldDocumentsAsync(id);
        await sut.DownloadDocumentAsync(id2);
        await sut.UpdateDocumentAsync(id, id2, new WorldDocumentUpdateDto { Title = "doc", Description = "d" });
        await sut.DeleteDocumentAsync(id, id2);

        Assert.Contains(calls, c => c == "GET worlds");
        Assert.Contains(calls, c => c.Contains($"GET worlds/{id}"));
        Assert.Contains(calls, c => c.Contains("POST worlds"));
        Assert.Contains(calls, c => c.Contains($"PUT worlds/{id}"));
        Assert.Contains(calls, c => c.Contains($"GET /documents/{id2}/content") || c.Contains($"GET documents/{id2}/content"));
    }

    [Fact]
    public async Task DownloadDocumentAsync_ReturnsNull_WhenNoInfo()
    {
        var sut = new WorldApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<WorldApiService>.Instance);

        var result = await sut.DownloadDocumentAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadDocumentAsync_ReturnsNull_WhenRequestThrows()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var sut = new WorldApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<WorldApiService>.Instance);

        var result = await sut.DownloadDocumentAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}

