using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ArticleCacheServiceTests
{
    [Fact]
    public async Task GetArticleInfoAsync_UsesCacheAfterFirstFetch()
    {
        var id = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        api.GetArticleAsync(id).Returns(new ArticleDto
        {
            Id = id,
            Title = "Title",
            Slug = "slug",
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" },
                new() { Id = id, Title = "Title", Slug = "slug" }
            }
        });

        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);

        var first = await sut.GetArticleInfoAsync(id);
        var second = await sut.GetArticleInfoAsync(id);

        Assert.NotNull(first);
        Assert.Same(first, second);
        await api.Received(1).GetArticleAsync(id);
    }

    [Fact]
    public async Task NavigationPath_UsesBreadcrumbsOrSlug()
    {
        var id = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        api.GetArticleAsync(id).Returns(new ArticleDto
        {
            Id = id,
            Title = "Title",
            Slug = "slug",
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" },
                new() { Id = id, Title = "Title", Slug = "slug" }
            }
        });
        api.GetArticleAsync(id2).Returns(new ArticleDto { Id = id2, Title = "T2", Slug = "slug-2", Breadcrumbs = new List<BreadcrumbDto>() });

        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);

        Assert.Equal("world/slug", await sut.GetNavigationPathAsync(id));
        Assert.Equal("slug-2", await sut.GetNavigationPathAsync(id2));
    }

    [Fact]
    public async Task CacheInvalidation_Works()
    {
        var id = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        api.GetArticleAsync(id).Returns(new ArticleDto { Id = id, Title = "t", Slug = "s" });

        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);
        await sut.GetArticleInfoAsync(id);

        sut.InvalidateArticle(id);
        await sut.GetArticleInfoAsync(id);

        sut.InvalidateCache();
        await sut.GetArticleInfoAsync(id);

        await api.Received(3).GetArticleAsync(id);
    }

    [Fact]
    public async Task HandlesNullAndExceptions()
    {
        var id = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        api.GetArticleAsync(id).Returns(Task.FromResult<ArticleDto?>(null));

        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);
        Assert.Null(await sut.GetArticleInfoAsync(id));
        Assert.Null(await sut.GetArticlePathAsync(id));

        api.GetArticleAsync(id).Returns(_ => Task.FromException<ArticleDto?>(new InvalidOperationException("boom")));
        Assert.Null(await sut.GetArticleInfoAsync(id));

        sut.CacheArticle(null!); // no-op branch
    }

    [Fact]
    public async Task CacheArticle_BuildsDisplayPath_AndHandlesMissingBreadcrumbs()
    {
        var id = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);

        sut.CacheArticle(new ArticleDto
        {
            Id = id,
            Title = "A",
            Slug = "a",
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" },
                new() { Id = id, Title = "Article", Slug = "article" }
            }
        });

        sut.CacheArticle(new ArticleDto
        {
            Id = id2,
            Title = "B",
            Slug = "b",
            Breadcrumbs = null
        });

        Assert.Equal("Article", (await sut.GetArticleInfoAsync(id))!.DisplayPath);
        Assert.Equal("Article", await sut.GetArticlePathAsync(id));
        Assert.Equal("b", await sut.GetNavigationPathAsync(id2));
        sut.InvalidateArticle(Guid.NewGuid()); // remove miss branch
    }

    [Fact]
    public async Task GetNavigationPathAsync_ReturnsNull_WhenArticleMissing()
    {
        var api = Substitute.For<IArticleApiService>();
        api.GetArticleAsync(Arg.Any<Guid>()).Returns((ArticleDto?)null);
        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);

        var result = await sut.GetNavigationPathAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void TryGetCachedArticle_ReturnsExpectedResult()
    {
        var id = Guid.NewGuid();
        var api = Substitute.For<IArticleApiService>();
        var sut = new ArticleCacheService(api, NullLogger<ArticleCacheService>.Instance);
        var dto = new ArticleDto
        {
            Id = id,
            Title = "Cached",
            Slug = "cached"
        };

        Assert.False(sut.TryGetCachedArticle(id, out var missing));
        Assert.Null(missing);

        sut.CacheArticle(dto);

        Assert.True(sut.TryGetCachedArticle(id, out var cached));
        Assert.Same(dto, cached);
    }
}

