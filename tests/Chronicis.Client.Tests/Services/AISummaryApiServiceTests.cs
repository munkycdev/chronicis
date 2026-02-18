using System.Net;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class AISummaryApiServiceTests
{
    [Fact]
    public async Task TemplateAndEntityMethods_UseExpectedRoutes()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new AISummaryApiService(http, NullLogger<AISummaryApiService>.Instance);
        var id = Guid.NewGuid();

        await sut.GetTemplatesAsync();
        await sut.GetEstimateAsync(id);
        await sut.GenerateSummaryAsync(id);
        await sut.GetSummaryAsync(id);
        await sut.ClearSummaryAsync(id);
        await sut.GetEntitySummaryAsync("campaign", id);
        await sut.GetEntityEstimateAsync("arc", id);
        await sut.GenerateEntitySummaryAsync("campaign", id, new GenerateSummaryRequestDto());
        await sut.ClearEntitySummaryAsync("arc", id);

        Assert.Contains(calls, c => c == "summary/templates");
        Assert.Contains(calls, c => c == $"articles/{id}/summary/estimate");
        Assert.Contains(calls, c => c == $"articles/{id}/summary/generate");
        Assert.Contains(calls, c => c == $"campaigns/{id}/summary");
        Assert.Contains(calls, c => c == $"arcs/{id}/summary/estimate");
    }

    [Fact]
    public async Task GetSummaryPreviewAsync_HandlesStatusesAndExceptions()
    {
        var noContent = new AISummaryApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.NoContent), NullLogger<AISummaryApiService>.Instance);
        var notFound = new AISummaryApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound), NullLogger<AISummaryApiService>.Instance);
        var bad = new AISummaryApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<AISummaryApiService>.Instance);
        var ok = new AISummaryApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}"), NullLogger<AISummaryApiService>.Instance);
        var ex = new AISummaryApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<AISummaryApiService>.Instance);

        Assert.Null(await noContent.GetSummaryPreviewAsync(Guid.NewGuid()));
        Assert.Null(await notFound.GetSummaryPreviewAsync(Guid.NewGuid()));
        Assert.Null(await bad.GetSummaryPreviewAsync(Guid.NewGuid()));
        Assert.NotNull(await ok.GetSummaryPreviewAsync(Guid.NewGuid()));
        Assert.Null(await ex.GetSummaryPreviewAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    public async Task EntityMethods_Throw_OnUnknownType(string entityType)
    {
        var sut = new AISummaryApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<AISummaryApiService>.Instance);
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetEntitySummaryAsync(entityType, id));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetEntityEstimateAsync(entityType, id));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GenerateEntitySummaryAsync(entityType, id));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.ClearEntitySummaryAsync(entityType, id));
    }
}

