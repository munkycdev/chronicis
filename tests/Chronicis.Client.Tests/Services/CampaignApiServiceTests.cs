using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class CampaignApiServiceTests
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
        var sut = new CampaignApiService(http, NullLogger<CampaignApiService>.Instance);
        var id = Guid.NewGuid();

        await sut.GetCampaignAsync(id);
        await sut.CreateCampaignAsync(new() { Name = "Campaign", WorldId = Guid.NewGuid() });
        await sut.UpdateCampaignAsync(id, new() { Name = "Updated" });
        await sut.GetActiveContextAsync(id);
        await sut.ActivateCampaignAsync(id);

        Assert.Contains(calls, c => c.Contains($"GET campaigns/{id}"));
        Assert.Contains(calls, c => c == "POST campaigns");
        Assert.Contains(calls, c => c.Contains($"PUT campaigns/{id}"));
        Assert.Contains(calls, c => c.Contains($"GET worlds/{id}/active-context"));
        Assert.Contains(calls, c => c.Contains($"POST campaigns/{id}/activate"));
    }

    [Fact]
    public async Task ActivateCampaignAsync_ReturnsFalse_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new CampaignApiService(http, NullLogger<CampaignApiService>.Instance);

        var result = await sut.ActivateCampaignAsync(Guid.NewGuid());

        Assert.False(result);
    }
}

