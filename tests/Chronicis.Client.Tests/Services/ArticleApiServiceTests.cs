using System.Net;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ArticleApiServiceTests
{
    [Fact]
    public async Task Routes_AndBranches_WorkAsExpected()
    {
        string? lastPath = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            lastPath = req.RequestUri!.PathAndQuery.TrimStart('/');
            HttpResponseMessage response;
            if (lastPath.StartsWith("articles/search"))
            {
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"titleMatches\":[],\"bodyMatches\":[],\"hashtagMatches\":[]}")
                };
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }

            return Task.FromResult(response);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new ArticleApiService(http, NullLogger<ArticleApiService>.Instance);
        var id = Guid.NewGuid();

        await sut.GetRootArticlesAsync();
        Assert.Equal("articles", lastPath);
        await sut.GetRootArticlesAsync(id);
        Assert.Contains($"articles?worldId={id}", lastPath);

        await sut.GetAllArticlesAsync();
        Assert.Equal("articles/all", lastPath);
        await sut.GetAllArticlesAsync(id);
        Assert.Contains($"articles/all?worldId={id}", lastPath);

        await sut.GetChildrenAsync(id);
        await sut.GetArticleDetailAsync(id);
        await sut.GetArticleAsync(id);
        await sut.GetArticleByPathAsync("a b/c");
        Assert.Contains("articles/by-path/a%20b/c", lastPath);

        await sut.CreateArticleAsync(new ArticleCreateDto { Title = "t", Type = Shared.Enums.ArticleType.WikiArticle });
        await sut.UpdateArticleAsync(id, new ArticleUpdateDto { Title = "t", Body = "b", Type = Shared.Enums.ArticleType.WikiArticle });
        await sut.DeleteArticleAsync(id);
        await sut.MoveArticleAsync(id, Guid.NewGuid());

        var empty = await sut.SearchArticlesAsync(" ");
        Assert.Empty(empty);

        var results = await sut.SearchArticlesAsync("test");
        Assert.Empty(results);

        var titleEmpty = await sut.SearchArticlesByTitleAsync(" ");
        Assert.Empty(titleEmpty);

        var titleResults = await sut.SearchArticlesByTitleAsync("test");
        Assert.Empty(titleResults);

        await sut.UpdateAliasesAsync(id, null!);
    }

    [Fact]
    public async Task SearchArticlesAsync_ReturnsEmpty_WhenApiReturnsNull()
    {
        var sut = new ArticleApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<ArticleApiService>.Instance);

        var result = await sut.SearchArticlesAsync("query");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchArticlesByTitleAsync_ReturnsEmpty_WhenApiReturnsNull()
    {
        var sut = new ArticleApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<ArticleApiService>.Instance);

        var result = await sut.SearchArticlesByTitleAsync("query");

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAliasesAsync_UsesProvidedAlias()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });
        var sut = new ArticleApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<ArticleApiService>.Instance);
        var id = Guid.NewGuid();

        await sut.UpdateAliasesAsync(id, "alias");

        Assert.Contains($"articles/{id}/aliases", calls);
    }
}

