using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ArticleExternalLinkApiServiceTests
{
    [Fact]
    public async Task GetExternalLinksAsync_ReturnsEmpty_OnNullOrException()
    {
        var nullService = new ArticleExternalLinkApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<ArticleExternalLinkApiService>.Instance);
        var exService = new ArticleExternalLinkApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ArticleExternalLinkApiService>.Instance);

        Assert.Empty(await nullService.GetExternalLinksAsync(Guid.NewGuid()));
        Assert.Empty(await exService.GetExternalLinksAsync(Guid.NewGuid()));
    }
}

