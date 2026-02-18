using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ArcApiServiceTests
{
    [Fact]
    public async Task Methods_UseExpectedRoutes()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new ArcApiService(http, NullLogger<ArcApiService>.Instance);
        var id = Guid.NewGuid();

        await sut.GetArcsByCampaignAsync(id);
        await sut.GetArcAsync(id);
        await sut.CreateArcAsync(new() { Name = "Arc", CampaignId = Guid.NewGuid() });
        await sut.UpdateArcAsync(id, new() { Name = "Arc" });
        await sut.DeleteArcAsync(id);
        await sut.ActivateArcAsync(id);

        Assert.Contains(calls, c => c.Contains($"GET campaigns/{id}/arcs"));
        Assert.Contains(calls, c => c.Contains($"GET arcs/{id}"));
        Assert.Contains(calls, c => c == "POST arcs");
        Assert.Contains(calls, c => c.Contains($"PUT arcs/{id}"));
        Assert.Contains(calls, c => c.Contains($"DELETE arcs/{id}"));
        Assert.Contains(calls, c => c.Contains($"POST arcs/{id}/activate"));
    }

    [Fact]
    public async Task ActivateArcAsync_ReturnsFalse_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new ArcApiService(http, NullLogger<ArcApiService>.Instance);

        var result = await sut.ActivateArcAsync(Guid.NewGuid());

        Assert.False(result);
    }
}

