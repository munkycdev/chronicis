using System.Net;
using System.Text.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class AdminApiServiceTests
{
    private static AdminApiService CreateSut(HttpClient http)
        => new(http, NullLogger<AdminApiService>.Instance);

    // ── GetWorldSummariesAsync ──────────────────────────────────────

    [Fact]
    public async Task GetWorldSummaries_CallsCorrectRoute()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.GetWorldSummariesAsync();

        Assert.Single(calls, c => c == "GET admin/worlds");
    }

    [Fact]
    public async Task GetWorldSummaries_ReturnsDeserializedList()
    {
        var summaries = new List<AdminWorldSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", OwnerName = "Dave", OwnerEmail = "dave@test.com" },
            new() { Id = Guid.NewGuid(), Name = "Beta" }
        };
        var json = JsonSerializer.Serialize(summaries);
        var sut = CreateSut(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json));

        var result = await sut.GetWorldSummariesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public async Task GetWorldSummaries_ReturnsEmptyList_OnHttpError()
    {
        var sut = CreateSut(TestHttpMessageHandler.CreateClient(HttpStatusCode.InternalServerError));

        var result = await sut.GetWorldSummariesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWorldSummaries_ReturnsEmptyList_OnNetworkException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network error"));
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await sut.GetWorldSummariesAsync();

        Assert.Empty(result);
    }

    // ── DeleteWorldAsync ────────────────────────────────────────────

    [Fact]
    public async Task DeleteWorld_CallsCorrectRoute()
    {
        var worldId = Guid.NewGuid();
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.DeleteWorldAsync(worldId);

        Assert.Single(calls, c => c == $"DELETE admin/worlds/{worldId}");
    }

    [Fact]
    public async Task DeleteWorld_ReturnsTrue_OnSuccess()
    {
        var sut = CreateSut(TestHttpMessageHandler.CreateClient(HttpStatusCode.NoContent));

        var result = await sut.DeleteWorldAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteWorld_ReturnsFalse_OnNotFound()
    {
        var sut = CreateSut(TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound));

        var result = await sut.DeleteWorldAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteWorld_ReturnsFalse_OnNetworkException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("boom"));
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await sut.DeleteWorldAsync(Guid.NewGuid());

        Assert.False(result);
    }

    // ── Tutorial mappings ──────────────────────────────────────────

    [Fact]
    public async Task GetTutorialMappings_CallsCorrectRoute()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.GetTutorialMappingsAsync();

        Assert.Single(calls, c => c == "GET sysadmin/tutorials");
    }

    [Fact]
    public async Task CreateTutorialMapping_CallsCorrectRouteAndReturnsEntity()
    {
        var calls = new List<string>();
        var body = JsonSerializer.Serialize(new TutorialMappingDto
        {
            Id = Guid.NewGuid(),
            PageType = "Page:Dashboard",
            PageTypeName = "Dashboard",
            Title = "Dashboard Tutorial"
        });
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        });
        var sut = CreateSut(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await sut.CreateTutorialMappingAsync(new TutorialMappingCreateDto
        {
            PageType = "Page:Dashboard",
            PageTypeName = "Dashboard",
            Title = "Dashboard Tutorial"
        });

        Assert.NotNull(result);
        Assert.Equal("Page:Dashboard", result!.PageType);
        Assert.Single(calls, c => c == "POST sysadmin/tutorials");
    }
}
