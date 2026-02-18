using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class LinkApiServiceTests
{
    [Fact]
    public async Task Methods_ReturnFallbacks_OnNullResponses()
    {
        var sut = new LinkApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<LinkApiService>.Instance);
        var id = Guid.NewGuid();

        Assert.Empty(await sut.GetSuggestionsAsync(id, "query"));
        Assert.Empty(await sut.GetBacklinksAsync(id));
        Assert.Empty(await sut.GetOutgoingLinksAsync(id));
        Assert.Empty(await sut.ResolveLinksAsync(new List<Guid> { id }));
        Assert.Null(await sut.AutoLinkAsync(id, "body"));
    }

    [Fact]
    public async Task ResolveLinksAsync_ReturnsEmpty_WhenInputEmpty()
    {
        var sut = new LinkApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}"), NullLogger<LinkApiService>.Instance);

        var result = await sut.ResolveLinksAsync(new List<Guid>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task Methods_ReturnMappedData_WhenResponsePresent()
    {
        var sut = new LinkApiService(
            TestHttpMessageHandler.CreateClient(req =>
            {
                var path = req.RequestUri!.PathAndQuery;
                if (path.Contains("link-suggestions"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"suggestions\":[{\"articleId\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"T\"}]}")
                    };
                }

                if (path.Contains("backlinks"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"backlinks\":[{\"articleId\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"B\"}]}")
                    };
                }

                if (path.Contains("outgoing-links"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"backlinks\":[{\"articleId\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"O\"}]}")
                    };
                }

                if (path.Contains("resolve-links"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"articles\":{\"11111111-1111-1111-1111-111111111111\":{\"articleId\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"R\"}}}")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            }),
            NullLogger<LinkApiService>.Instance);

        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Single(await sut.GetSuggestionsAsync(id, "q"));
        Assert.Single(await sut.GetBacklinksAsync(id));
        Assert.Single(await sut.GetOutgoingLinksAsync(id));
        Assert.Single(await sut.ResolveLinksAsync(new List<Guid> { id }));
    }
}

