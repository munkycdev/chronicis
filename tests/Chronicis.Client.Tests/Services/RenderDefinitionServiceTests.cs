using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class RenderDefinitionServiceTests
{
    [Fact]
    public async Task ResolveAsync_UsesMostSpecificThenFallbacks()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery.TrimStart('/');
            calls.Add(path);

            if (path == "render-definitions/ros/bestiary/Cultural-Being.json")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            if (path == "render-definitions/ros/bestiary.json")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"titleField\":\"name\",\"sections\":[]}")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = new RenderDefinitionService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<RenderDefinitionService>.Instance);

        var result = await sut.ResolveAsync("ros", "bestiary/Cultural-Being");

        Assert.NotNull(result);
        Assert.Equal("name", result.TitleField);
        Assert.Equal("render-definitions/ros/bestiary/Cultural-Being.json", calls[0]);
        Assert.Equal("render-definitions/ros/bestiary.json", calls[1]);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsBuiltInDefault_WhenNoneFound()
    {
        var sut = new RenderDefinitionService(TestHttpMessageHandler.CreateClient(HttpStatusCode.NotFound), NullLogger<RenderDefinitionService>.Instance);

        var result = await sut.ResolveAsync("ros", null);

        Assert.NotNull(result);
        Assert.True(result.CatchAll);
        Assert.Equal("name", result.TitleField);
    }

    [Fact]
    public async Task ResolveAsync_UsesCache_ForSuccessfulAndFailedLoads()
    {
        var hitCount = 0;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            hitCount++;
            var path = req.RequestUri!.PathAndQuery.TrimStart('/');
            if (path == "render-definitions/ros.json")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"titleField\":\"cached\",\"sections\":[]}")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
        var sut = new RenderDefinitionService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<RenderDefinitionService>.Instance);

        var one = await sut.ResolveAsync("ros", null);
        var two = await sut.ResolveAsync("ros", null);

        Assert.Equal("cached", one.TitleField);
        Assert.Equal("cached", two.TitleField);
        Assert.Equal(1, hitCount);
    }

    [Fact]
    public async Task ResolveAsync_HandlesHttpAndParseExceptions()
    {
        var httpExceptionService = new RenderDefinitionService(
            new HttpClient(new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network")))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            NullLogger<RenderDefinitionService>.Instance);

        var parseExceptionService = new RenderDefinitionService(
            TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{invalid-json"),
            NullLogger<RenderDefinitionService>.Instance);

        var a = await httpExceptionService.ResolveAsync("ros", "x");
        var b = await parseExceptionService.ResolveAsync("ros", "x");

        Assert.True(a.CatchAll);
        Assert.True(b.CatchAll);
    }
}

