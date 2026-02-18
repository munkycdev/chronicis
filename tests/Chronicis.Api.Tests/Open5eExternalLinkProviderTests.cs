using System.Diagnostics.CodeAnalysis;
using System.Net;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class Open5eExternalLinkProviderTests
{
    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsCategorySuggestions()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var result = await sut.SearchAsync("", CancellationToken.None);

            Assert.NotEmpty(result);
            Assert.Contains(result, x => x.Id == "_category/spells");
            Assert.All(result, x => Assert.Equal("srd", x.Source));
        }
    }

    [Fact]
    public async Task SearchAsync_PartialCategoryWithoutSlash_ReturnsMatchingCategoriesOnly()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var result = await sut.SearchAsync("spe", CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("_category/spells", result[0].Id);
        }
    }

    [Fact]
    public async Task SearchAsync_CategoryWithNoSearchTerm_ReturnsEmpty()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var result = await sut.SearchAsync("spells/", CancellationToken.None);

            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_CategoryAndTerm_CallsExpectedEndpoint_AndFiltersByName()
    {
        Uri? requestedUri = null;
        var (sut, disposable) = CreateSut(request =>
        {
            requestedUri = request.RequestUri;
            return StubHttpMessageHandler.JsonResponse("""
            {
              "results": [
                { "key": "fireball", "name": "Fireball", "level": 3, "school": { "name": "Evocation" } },
                { "key": "ice-storm", "name": "Ice Storm", "level": 4, "school": { "name": "Evocation" } }
              ]
            }
            """);
        });
        using (disposable)
        {
            var result = await sut.SearchAsync("spells/fire", CancellationToken.None);

            Assert.NotNull(requestedUri);
            Assert.Equal("/v2/spells/", requestedUri!.AbsolutePath);
            var query = requestedUri.Query;
            Assert.Contains("name__contains=fire", query);
            Assert.Contains("document__gamesystem__key=5e-2014", query);
            Assert.Contains("limit=50", query);

            Assert.Single(result);
            Assert.Equal("spells/fireball", result[0].Id);
            Assert.Equal("Fireball", result[0].Title);
            Assert.Equal("spells", result[0].Category);
        }
    }

    [Fact]
    public async Task SearchAsync_SortsAndCapsTo20()
    {
        var payload = "{\"results\": [" + string.Join(",", Enumerable.Range(0, 30).Select(i =>
            $"{{\"key\":\"spell-{i}\",\"name\":\"Spell {i:D2}\",\"level\":1,\"school\":{{\"name\":\"Evocation\"}}}}")) + "]}";

        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse(payload));
        using (disposable)
        {
            var result = await sut.SearchAsync("spells/spell", CancellationToken.None);

            Assert.Equal(20, result.Count);
            var titles = result.Select(x => x.Title).ToList();
            var sorted = titles.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.Equal(sorted, titles);
        }
    }

    [Fact]
    public async Task GetContentAsync_InvalidId_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{}"));
        using (disposable)
        {
            var content = await sut.GetContentAsync("bad-id", CancellationToken.None);

            Assert.Equal("srd", content.Source);
            Assert.Equal("bad-id", content.Id);
            Assert.Equal(string.Empty, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_UnknownCategory_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{}"));
        using (disposable)
        {
            var content = await sut.GetContentAsync("unknown/fireball", CancellationToken.None);

            Assert.Equal("unknown/fireball", content.Id);
            Assert.Equal(string.Empty, content.Title);
            Assert.Equal(string.Empty, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_NonSuccessStatus_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/fireball", CancellationToken.None);

            Assert.Equal(string.Empty, content.Title);
            Assert.Equal(string.Empty, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_HttpFailure_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => throw new HttpRequestException("boom"));
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/fireball", CancellationToken.None);

            Assert.Equal(string.Empty, content.Title);
            Assert.Equal(string.Empty, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_ValidSpell_ReturnsMappedContent()
    {
        Uri? requestedUri = null;
        var (sut, disposable) = CreateSut(request =>
        {
            requestedUri = request.RequestUri;
            return StubHttpMessageHandler.JsonResponse("""
            {
              "name": "Fireball",
              "level": 3,
              "school": { "name": "Evocation" },
              "casting_time": "1 action",
              "range": "150 feet",
              "duration": "Instantaneous",
              "desc": "A bright streak flashes.",
              "document": { "name": "System Reference Document 5.1" }
            }
            """);
        });
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/fireball", CancellationToken.None);

            Assert.NotNull(requestedUri);
            Assert.Equal("/v2/spells/fireball/", requestedUri!.AbsolutePath);

            Assert.Equal("srd", content.Source);
            Assert.Equal("spells/fireball", content.Id);
            Assert.Equal("Fireball", content.Title);
            Assert.Equal("Spell", content.Kind);
            Assert.Contains("# Fireball", content.Markdown);
            Assert.Contains("## Casting", content.Markdown);
            Assert.Contains("## Description", content.Markdown);
            Assert.Equal("Source: System Reference Document 5.1", content.Attribution);
            Assert.Equal("https://open5e.com/spells/fireball", content.ExternalUrl);
        }
    }

    private static (Open5eExternalLinkProvider Sut, IDisposable Disposable) CreateSut(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpHandler = new StubHttpMessageHandler(handler);
        var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("https://example.test")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Open5eApi").Returns(httpClient);

        return (
            new Open5eExternalLinkProvider(factory, NullLogger<Open5eExternalLinkProvider>.Instance),
            new AggregateDisposable(httpClient, httpHandler));
    }

    private sealed class AggregateDisposable : IDisposable
    {
        private readonly IDisposable[] _items;

        public AggregateDisposable(params IDisposable[] items)
        {
            _items = items;
        }

        public void Dispose()
        {
            foreach (var item in _items)
            {
                item.Dispose();
            }
        }
    }
}
