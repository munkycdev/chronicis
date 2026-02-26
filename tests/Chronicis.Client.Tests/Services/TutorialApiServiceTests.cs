using System.Net;
using System.Text.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class TutorialApiServiceTests
{
    [Fact]
    public async Task ResolveAsync_WithWhitespace_ReturnsNull()
    {
        var sut = new TutorialApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<TutorialApiService>.Instance);

        var result = await sut.ResolveAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_CallsEncodedRouteAndReturnsEntity()
    {
        var calls = new List<string>();
        var dto = new TutorialDto { ArticleId = Guid.NewGuid(), Title = "Dashboard Tutorial", Body = "Body" };
        var json = JsonSerializer.Serialize(dto);
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        });
        var sut = new TutorialApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<TutorialApiService>.Instance);

        var result = await sut.ResolveAsync("Page:Admin Utilities");

        Assert.NotNull(result);
        Assert.Equal("Dashboard Tutorial", result!.Title);
        Assert.Single(calls, c => c == "GET tutorials/resolve?pageType=Page%3AAdmin%20Utilities");
    }
}
