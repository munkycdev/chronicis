using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class SearchApiServiceTests
{
    [Fact]
    public async Task SearchContentAsync_ReturnsNull_WhenQueryBlank()
    {
        var sut = new SearchApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<SearchApiService>.Instance);

        var result = await sut.SearchContentAsync(" ");

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchContentAsync_UsesEncodedQuery()
    {
        string? path = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            path = req.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new SearchApiService(http, NullLogger<SearchApiService>.Instance);

        await sut.SearchContentAsync("a b");

        Assert.Contains("search?query=a+b", path);
    }
}

