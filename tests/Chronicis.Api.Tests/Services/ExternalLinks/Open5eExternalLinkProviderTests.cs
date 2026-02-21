using System.Diagnostics.CodeAnalysis;
using System.Net;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services.ExternalLinks;

[ExcludeFromCodeCoverage]
public class Open5eExternalLinkProviderTests
{
    [Fact]
    public void Key_ReturnsSrd()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{}"));
        using (disposable)
        {
            Assert.Equal("srd", sut.Key);
        }
    }

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
            // Verify icons come from strategies
            var spells = result.First(x => x.Id == "_category/spells");
            Assert.Equal("✨", spells.Icon);
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
    public async Task SearchAsync_IgnoresItemsMissingRequiredFields()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "results": [
            { "name": "Fireball", "level": 3, "school": { "name": "Evocation" } },
            { "key": "fire-bolt", "level": 0, "school": { "name": "Evocation" } }
          ]
        }
        """));

        using (disposable)
        {
            var result = await sut.SearchAsync("spells/fire", CancellationToken.None);
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_UnknownCategoryWithSlash_ReturnsFilteredCategories()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var result = await sut.SearchAsync("zzz/fire", CancellationToken.None);
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_CategorySearch_NonSuccess_ReturnsEmpty()
    {
        var (sut, disposable) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using (disposable)
        {
            var result = await sut.SearchAsync("spells/fire", CancellationToken.None);
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_CategorySearch_Exception_ReturnsEmpty()
    {
        var (sut, disposable) = CreateSut(_ => throw new HttpRequestException("boom"));
        using (disposable)
        {
            var result = await sut.SearchAsync("spells/fire", CancellationToken.None);
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_PrefixMatch_ResolvesCategory()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {"results":[{"key":"hydra","name":"Hydra","challenge_rating":"8","type":"monstrosity"}]}
        """));
        using (disposable)
        {
            // "mon" should prefix-match to "monsters"
            var result = await sut.SearchAsync("mon/hydra", CancellationToken.None);
            Assert.Single(result);
            Assert.Equal("monsters/hydra", result[0].Id);
        }
    }

    [Fact]
    public async Task SearchAsync_SubtitleDelegatedToStrategy()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {"results":[{"key":"hydra","name":"Hydra","challenge_rating":"8","type":"monstrosity"}]}
        """));
        using (disposable)
        {
            var result = await sut.SearchAsync("monsters/hyd", CancellationToken.None);
            Assert.Equal("Monster • CR 8 • monstrosity", Assert.Single(result).Subtitle);
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
    public async Task GetContentAsync_NullId_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{}"));
        using (disposable)
        {
            var content = await sut.GetContentAsync(null!, CancellationToken.None);
            Assert.Equal("srd", content.Source);
            Assert.Equal(string.Empty, content.Title);
            Assert.Equal(string.Empty, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_EmptyId_ReturnsEmptyContent()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{}"));
        using (disposable)
        {
            var content = await sut.GetContentAsync("", CancellationToken.None);
            Assert.Equal(string.Empty, content.Id);
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
    public async Task GetContentAsync_ValidSpell_DelegatesToStrategy()
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
            Assert.Equal("Source: System Reference Document 5.1", content.Attribution);
            Assert.Equal("https://open5e.com/spells/fireball", content.ExternalUrl);
        }
    }

    [Theory]
    [InlineData("monsters", "hydra", "Monster", "## Statistics", "https://open5e.com/monsters/hydra")]
    [InlineData("magicitems", "wand", "Magic Item", "# Wand", "https://open5e.com/magic-items/wand")]
    [InlineData("conditions", "blinded", "Condition", "# Blinded", "https://open5e.com/conditions/blinded")]
    [InlineData("backgrounds", "acolyte", "Background", "# Acolyte", "https://open5e.com/backgrounds/acolyte")]
    [InlineData("feats", "alert", "Feat", "# Alert", "https://open5e.com/feats/alert")]
    [InlineData("classes", "wizard", "Class", "# Wizard", "https://open5e.com/classes/wizard")]
    [InlineData("races", "elf", "Race", "# Elf", "https://open5e.com/races/elf")]
    [InlineData("weapons", "longsword", "Weapon", "# Longsword", "https://open5e.com/weapons/longsword")]
    [InlineData("armor", "chain-mail", "Armor", "# Chain Mail", "https://open5e.com/armor/chain-mail")]
    public async Task GetContentAsync_AllCategories_DelegateToCorrectStrategy(
        string category, string key, string kind, string markdownSnippet, string expectedUrl)
    {
        var json = BuildCategoryJson(category);
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse(json));
        using (disposable)
        {
            var content = await sut.GetContentAsync($"{category}/{key}", CancellationToken.None);
            Assert.Equal(kind, content.Kind);
            Assert.Equal(expectedUrl, content.ExternalUrl);
            Assert.Contains(markdownSnippet, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_UsesDocumentTitleFallback_WhenDocumentObjectMissing()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {"name": "Test", "desc": "d", "document__title": "Custom SRD"}
        """));
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/test", CancellationToken.None);
            Assert.Equal("Source: Custom SRD", content.Attribution);
        }
    }

    [Fact]
    public async Task GetContentAsync_UsesDefaultAttribution_WhenNoDocumentFields()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {"name": "Test", "desc": "d"}
        """));
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/test", CancellationToken.None);
            Assert.Equal("Source: System Reference Document 5.1", content.Attribution);
        }
    }

    [Fact]
    public async Task GetContentAsync_MissingName_FallsBackToItemKey()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""{"desc":"d"}"""));
        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/my-spell", CancellationToken.None);
            Assert.Equal("my-spell", content.Title);
        }
    }

    // --- Helper methods ---

    private static (Open5eExternalLinkProvider Sut, IDisposable Disposable) CreateSut(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
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

    private static string BuildCategoryJson(string category) => category switch
    {
        "monsters" => """
        {
          "name": "Hydra", "size": { "name": "Huge" }, "type": { "name": "Monstrosity" },
          "alignment": "unaligned", "armor_class": "15", "hit_points": "172",
          "challenge_rating": "8", "speed": { "walk": "30 ft" },
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "magicitems" => """{"name":"Wand","type":"Wand","rarity":"Uncommon","desc":"Has charges.","document":{"name":"System Reference Document 5.1"}}""",
        "conditions" => """{"name":"Blinded","desc":"Can't see.","document":{"name":"System Reference Document 5.1"}}""",
        "backgrounds" => """{"name":"Acolyte","desc":"Service.","document":{"name":"System Reference Document 5.1"}}""",
        "feats" => """{"name":"Alert","desc":"Quick.","document":{"name":"System Reference Document 5.1"}}""",
        "classes" => """{"name":"Wizard","hit_dice":"1d6","desc":"Magic.","document":{"name":"System Reference Document 5.1"}}""",
        "races" => """{"name":"Elf","size":"Medium","speed":"30 ft","desc":"Magical.","document":{"name":"System Reference Document 5.1"}}""",
        "weapons" => """{"name":"Longsword","category":"Martial","damage_dice":"1d8","document":{"name":"System Reference Document 5.1"}}""",
        "armor" => """{"name":"Chain Mail","category":"Heavy","base_ac":"16","document":{"name":"System Reference Document 5.1"}}""",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    private sealed class AggregateDisposable : IDisposable
    {
        private readonly IDisposable[] _items;
        public AggregateDisposable(params IDisposable[] items) => _items = items;
        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();
        }
    }
}
