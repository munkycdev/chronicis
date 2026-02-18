using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task GetContentAsync_SpellWithComponentsAndHigherLevel_RendersBranches()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "name": "Aid",
          "level": 2,
          "school": { "name": "Abjuration" },
          "casting_time": "1 action",
          "range_text": "30 feet",
          "duration": "8 hours",
          "concentration": true,
          "ritual": "yes",
          "verbal": true,
          "somatic": true,
          "material": true,
          "material_specified": "a tiny strip of white cloth",
          "desc": "Bolster your allies.",
          "higher_level": "Increases by 5 hit points per slot level above 2nd."
        }
        """));

        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/aid", CancellationToken.None);
            Assert.Contains("- **Components:** V, S, M (a tiny strip of white cloth)", content.Markdown);
            Assert.Contains("## At Higher Levels", content.Markdown);
            Assert.Contains("(ritual)", content.Markdown);
            Assert.Contains("Concentration, 8 hours", content.Markdown);
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

    [Theory]
    [InlineData("monsters", "hydra", "Monster", "## Statistics", "https://open5e.com/monsters/hydra")]
    [InlineData("magicitems", "wand-of-magic-missiles", "Magic Item", "# Wand of Magic Missiles", "https://open5e.com/magic-items/wand-of-magic-missiles")]
    [InlineData("conditions", "blinded", "Condition", "# Blinded", "https://open5e.com/conditions/blinded")]
    [InlineData("backgrounds", "acolyte", "Background", "**Skill Proficiencies:**", "https://open5e.com/backgrounds/acolyte")]
    [InlineData("feats", "alert", "Feat", "*Prerequisite:", "https://open5e.com/feats/alert")]
    [InlineData("classes", "wizard", "Class", "**Hit Die:**", "https://open5e.com/classes/wizard")]
    [InlineData("races", "elf", "Race", "## Traits", "https://open5e.com/races/elf")]
    [InlineData("weapons", "longsword", "Weapon", "**Properties:**", "https://open5e.com/weapons/longsword")]
    [InlineData("armor", "chain-mail", "Armor", "**Armor Class:**", "https://open5e.com/armor/chain-mail")]
    public async Task GetContentAsync_CategorySpecificContent_MapsExpectedFields(
        string category,
        string key,
        string kind,
        string markdownSnippet,
        string expectedUrl)
    {
        var json = BuildCategoryJson(category);
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse(json));
        using (disposable)
        {
            var content = await sut.GetContentAsync($"{category}/{key}", CancellationToken.None);

            Assert.Equal(kind, content.Kind);
            Assert.Equal(expectedUrl, content.ExternalUrl);
            Assert.Contains(markdownSnippet, content.Markdown);
            Assert.Equal("Source: System Reference Document 5.1", content.Attribution);
        }
    }

    [Fact]
    public async Task GetContentAsync_UsesDocumentTitleFallback_WhenDocumentObjectMissing()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "name": "Test Spell",
          "desc": "desc",
          "document__title": "Custom SRD"
        }
        """));

        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/test-spell", CancellationToken.None);
            Assert.Equal("Source: Custom SRD", content.Attribution);
        }
    }

    [Fact]
    public async Task GetContentAsync_UsesDefaultAttribution_WhenNoDocumentFields()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "name": "Test Spell",
          "desc": "desc"
        }
        """));

        using (disposable)
        {
            var content = await sut.GetContentAsync("spells/test-spell", CancellationToken.None);
            Assert.Equal("Source: System Reference Document 5.1", content.Attribution);
        }
    }

    [Fact]
    public async Task GetContentAsync_MagicItemWithAttunement_IncludesAttunementInMarkdown()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "name": "Ring of Warmth",
          "type": "Ring",
          "rarity": "Rare",
          "requires_attunement": "requires attunement by a creature",
          "desc": "You have resistance to cold damage."
        }
        """));

        using (disposable)
        {
            var content = await sut.GetContentAsync("magicitems/ring-of-warmth", CancellationToken.None);
            Assert.Contains("(requires attunement by a creature)", content.Markdown);
        }
    }

    [Fact]
    public void PrivateHelpers_ParseQuery_And_AppendAbilityScores_Work()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var parseQuery = typeof(Open5eExternalLinkProvider).GetMethod(
                "ParseQuery",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var appendAbilityScores = typeof(Open5eExternalLinkProvider).GetMethod(
                "AppendAbilityScores",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

            var tuple1 = ((string? category, string searchTerm))parseQuery.Invoke(null, ["spells/fire"])!;
            Assert.Equal("spells", tuple1.category);
            Assert.Equal("fire", tuple1.searchTerm);

            var tuple2 = ((string? category, string searchTerm))parseQuery.Invoke(null, ["unknown/fire"])!;
            Assert.Null(tuple2.category);
            Assert.Equal("unknown/fire", tuple2.searchTerm);

            var tuple3 = ((string? category, string searchTerm))parseQuery.Invoke(null, ["spell/fire"])!;
            Assert.Equal("spells", tuple3.category);
            Assert.Equal("fire", tuple3.searchTerm);

            var tuple4 = ((string? category, string searchTerm))parseQuery.Invoke(null, ["spells/"])!;
            Assert.Null(tuple4.category);
            Assert.Equal("spells/", tuple4.searchTerm);

            var tuple5 = ((string? category, string searchTerm))parseQuery.Invoke(null, ["noslash"])!;
            Assert.Null(tuple5.category);
            Assert.Equal("noslash", tuple5.searchTerm);

            using var doc = JsonDocument.Parse("""
            {
              "strength": 18,
              "dexterity": 12,
              "constitution": 14,
              "intelligence": 8,
              "wisdom": 10,
              "charisma": 6
            }
            """);

            var sb = new StringBuilder();
            appendAbilityScores.Invoke(sut, [sb, doc.RootElement]);
            var markdown = sb.ToString();
            Assert.Contains("## Ability Scores", markdown);
            Assert.Contains("| STR | DEX | CON | INT | WIS | CHA |", markdown);
        }
    }

    [Fact]
    public async Task SearchAsync_CoversCategorySpecificSubtitleBranches()
    {
        var responses = new Queue<string>(
        [
            """{ "results": [ { "key":"hydra","name":"Hydra","challenge_rating":"8","type":"monstrosity" } ] }""",
            """{ "results": [ { "key":"wand","name":"Wand","rarity":"Uncommon","type":"Wand" } ] }""",
            """{ "results": [ { "key":"shield","name":"Shield","category":"Armor" } ] }"""
        ]);

        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse(responses.Dequeue()));
        using (disposable)
        {
            var monsters = await sut.SearchAsync("monsters/hyd", CancellationToken.None);
            Assert.Equal("Monster • CR 8 • monstrosity", Assert.Single(monsters).Subtitle);

            var items = await sut.SearchAsync("magicitems/wan", CancellationToken.None);
            Assert.Equal("Magic Item • Uncommon • Wand", Assert.Single(items).Subtitle);

            var armor = await sut.SearchAsync("armor/shi", CancellationToken.None);
            Assert.Equal("Armor • Armor", Assert.Single(armor).Subtitle);
        }
    }

    [Fact]
    public void PrivateHelpers_CoverRemainingBranches()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("{\"results\":[]}"));
        using (disposable)
        {
            var getCategoryIcon = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetCategoryIcon",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var buildMarkdown = typeof(Open5eExternalLinkProvider).GetMethod(
                "BuildMarkdown",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var appendNamedArray = typeof(Open5eExternalLinkProvider).GetMethod(
                "AppendNamedArray",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var getSpeedString = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetSpeedString",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var getString = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetString",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var getBool = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetBool",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var getStringArray = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetStringArray",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var appendAbilityScores = typeof(Open5eExternalLinkProvider).GetMethod(
                "AppendAbilityScores",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var parseSearchResult = typeof(Open5eExternalLinkProvider).GetMethod(
                "ParseSearchResult",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var buildSubtitle = typeof(Open5eExternalLinkProvider).GetMethod(
                "BuildSubtitle",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            var getInt = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetInt",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var getStringFromObject = typeof(Open5eExternalLinkProvider).GetMethod(
                "GetStringFromObject",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            Assert.Null(getCategoryIcon.Invoke(null, ["unknown"]));

            using var unknownDoc = JsonDocument.Parse("""{"desc":"fallback"}""");
            var unknownMarkdown = (string)buildMarkdown.Invoke(
                sut,
                [unknownDoc.RootElement, "Unknown", "not-a-category", null!])!;
            Assert.Contains("# Unknown", unknownMarkdown);

            var sb = new StringBuilder();
            using var noArrayDoc = JsonDocument.Parse("""{}""");
            appendNamedArray.Invoke(sut, [sb, noArrayDoc.RootElement, "actions", "Actions"]);
            Assert.Equal(string.Empty, sb.ToString());

            using var emptyArrayDoc = JsonDocument.Parse("""{"actions":[]}""");
            appendNamedArray.Invoke(sut, [sb, emptyArrayDoc.RootElement, "actions", "Actions"]);
            Assert.Equal(string.Empty, sb.ToString());

            using var speedStringDoc = JsonDocument.Parse("""{"speed":"30 ft"}""");
            Assert.Equal("30 ft", getSpeedString.Invoke(sut, [speedStringDoc.RootElement]));
            using var speedObjectNumberDoc = JsonDocument.Parse("""{"speed":{"walk":30}}""");
            Assert.Equal("walk 30", getSpeedString.Invoke(sut, [speedObjectNumberDoc.RootElement]));

            using var noSpeedDoc = JsonDocument.Parse("""{"name":"x"}""");
            Assert.Null(getSpeedString.Invoke(sut, [noSpeedDoc.RootElement]));

            using var notObjectDoc = JsonDocument.Parse("""[1,2,3]""");
            Assert.Null(getString.Invoke(null, [notObjectDoc.RootElement, "name"]));

            using var objectMissingPropDoc = JsonDocument.Parse("""{"name":"x"}""");
            Assert.Null(getString.Invoke(null, [objectMissingPropDoc.RootElement, "missing"]));

            using var boolDoc = JsonDocument.Parse("""{"a":true,"b":false,"c":"yes","d":"true"}""");
            Assert.Equal(true, getBool.Invoke(null, [boolDoc.RootElement, "a"]));
            Assert.Equal(false, getBool.Invoke(null, [boolDoc.RootElement, "b"]));
            Assert.Equal(true, getBool.Invoke(null, [boolDoc.RootElement, "c"]));
            Assert.Equal(true, getBool.Invoke(null, [boolDoc.RootElement, "d"]));
            Assert.Null(getBool.Invoke(null, [objectMissingPropDoc.RootElement, "missing"]));

            using var boolNoDoc = JsonDocument.Parse("""{"x":"no","n":123}""");
            Assert.Equal(false, getBool.Invoke(null, [boolNoDoc.RootElement, "x"]));
            Assert.Null(getBool.Invoke(null, [boolNoDoc.RootElement, "n"]));
            Assert.Null(getBool.Invoke(null, [notObjectDoc.RootElement, "x"]));

            var arrayMissing = (List<string>)getStringArray.Invoke(null, [notObjectDoc.RootElement, "props"])!;
            Assert.Empty(arrayMissing);

            using var arrayStringDoc = JsonDocument.Parse("""{"props":"Versatile"}""");
            var arrayString = (List<string>)getStringArray.Invoke(null, [arrayStringDoc.RootElement, "props"])!;
            Assert.Single(arrayString);
            Assert.Equal("Versatile", arrayString[0]);

            using var spellDoc = JsonDocument.Parse("""{"name":"Ray of Frost","level":0}""");
            var spellConfig = CreateCategoryConfig("spells", "5e-2014", "Spell");
            var spellSubtitle = (string)buildSubtitle.Invoke(sut, [spellDoc.RootElement, "spells", spellConfig])!;
            Assert.Equal("Spell • Cantrip", spellSubtitle);

            var unknownSubtitle = (string)buildSubtitle.Invoke(sut, [spellDoc.RootElement, "other", spellConfig])!;
            Assert.Equal("Spell", unknownSubtitle);

            using var missingNameDoc = JsonDocument.Parse("""{"key":"fireball"}""");
            Assert.Null(parseSearchResult.Invoke(sut, [missingNameDoc.RootElement, "spells", spellConfig]));

            using var intDoc = JsonDocument.Parse("""{"n":42}""");
            Assert.Equal(42, getInt.Invoke(null, [intDoc.RootElement, "n"]));
            Assert.Null(getInt.Invoke(null, [objectMissingPropDoc.RootElement, "n"]));
            Assert.Null(getInt.Invoke(null, [notObjectDoc.RootElement, "n"]));
            using var bigIntDoc = JsonDocument.Parse("""{"n":2147483648}""");
            Assert.Null(getInt.Invoke(null, [bigIntDoc.RootElement, "n"]));
            using var textIntDoc = JsonDocument.Parse("""{"n":"42"}""");
            Assert.Null(getInt.Invoke(null, [textIntDoc.RootElement, "n"]));

            using var childDoc = JsonDocument.Parse("""{"obj":{"name":"Inside"}}""");
            Assert.Equal("Inside", getStringFromObject.Invoke(null, [childDoc.RootElement, "obj", "name"]));
            Assert.Null(getStringFromObject.Invoke(null, [objectMissingPropDoc.RootElement, "obj", "name"]));
            Assert.Null(getStringFromObject.Invoke(null, [notObjectDoc.RootElement, "obj", "name"]));

            var sbNoAbility = new StringBuilder();
            using var noAbilityDoc = JsonDocument.Parse("""{"intelligence":18}""");
            appendAbilityScores.Invoke(sut, [sbNoAbility, noAbilityDoc.RootElement]);
            Assert.Equal(string.Empty, sbNoAbility.ToString());
        }
    }

    [Theory]
    [InlineData("spells/spark", """
    {
      "name": "Spark",
      "level": 0,
      "casting_time": "1 action",
      "range": "Self",
      "duration": "Instantaneous",
      "material": true,
      "desc": "A tiny spark appears."
    }
    """, "*Cantrip*", "- **Components:** M")]
    [InlineData("monsters/wolf", """
    {
      "name": "Wolf",
      "size": "Medium",
      "type": "Beast",
      "armor_class": "13",
      "hit_points": "11",
      "cr": "1/4",
      "speed": "40 ft",
      "actions": [{ "name":"Bite", "desc":"Attack." }]
    }
    """, "*Medium Beast*", "**Challenge:** 1/4")]
    [InlineData("weapons/club", """
    {
      "name": "Club",
      "category_range": "Simple Melee",
      "damage": "1d4",
      "cost": "1 sp",
      "weight": "2 lb."
    }
    """, "*Simple Melee*", "**Damage:** 1d4")]
    [InlineData("armor/hide", """
    {
      "name": "Hide Armor",
      "ac_string": "12 + Dex (max 2)",
      "stealth_disadvantage": "True"
    }
    """, "**Armor Class:** 12 + Dex (max 2)", "**Stealth:** Disadvantage")]
    public async Task GetContentAsync_CoversAdditionalMarkdownBranches(
        string id,
        string json,
        string expected1,
        string expected2)
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse(json));
        using (disposable)
        {
            var content = await sut.GetContentAsync(id, CancellationToken.None);
            Assert.Contains(expected1, content.Markdown);
            Assert.Contains(expected2, content.Markdown);
        }
    }

    [Fact]
    public async Task GetContentAsync_MagicItemWithoutAttunement_DoesNotAppendAttunement()
    {
        var (sut, disposable) = CreateSut(_ => StubHttpMessageHandler.JsonResponse("""
        {
          "name": "Driftglobe",
          "type": "Wondrous item",
          "rarity": "Uncommon",
          "desc": "Sheds light."
        }
        """));

        using (disposable)
        {
            var content = await sut.GetContentAsync("magicitems/driftglobe", CancellationToken.None);
            Assert.Contains("*Wondrous item, Uncommon*", content.Markdown);
            Assert.DoesNotContain("(requires attunement", content.Markdown, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static object CreateCategoryConfig(string endpoint, string documentSlug, string displayName)
    {
        var type = typeof(Open5eExternalLinkProvider).GetNestedType("CategoryConfig", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(type, endpoint, documentSlug, displayName)!;
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

    private static string BuildCategoryJson(string category) => category switch
    {
        "monsters" => """
        {
          "name": "Hydra",
          "size": { "name": "Huge" },
          "type": { "name": "Monstrosity" },
          "alignment": "unaligned",
          "armor_class": 15,
          "hit_points": 172,
          "hit_dice": "15d12+75",
          "challenge_rating": "8",
          "speed": { "walk": "30 ft", "swim": "30 ft" },
          "actions": [ { "name": "Bite", "desc": "Melee Weapon Attack." } ],
          "special_abilities": [ { "name": "Hold Breath", "desc": "Can hold breath for 1 hour." } ],
          "legendary_actions": [ { "name": "Regrow Head", "desc": "The hydra regrows one severed head." } ],
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "magicitems" => """
        {
          "name": "Wand of Magic Missiles",
          "type": "Wand",
          "rarity": "Uncommon",
          "requires_attunement": "false",
          "desc": "This wand has 7 charges.",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "conditions" => """
        {
          "name": "Blinded",
          "desc": "A blinded creature can't see.",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "backgrounds" => """
        {
          "name": "Acolyte",
          "desc": "You have spent your life in service of a temple.",
          "skill_proficiencies": "Insight, Religion",
          "equipment": "Holy symbol, prayer book",
          "feature": "Shelter of the Faithful",
          "feature_desc": "You and companions can expect free healing at a temple.",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "feats" => """
        {
          "name": "Alert",
          "prerequisite": "None",
          "desc": "Always on the lookout for danger.",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "classes" => """
        {
          "name": "Wizard",
          "hit_dice": "1d6",
          "desc": "A scholarly magic-user.",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "races" => """
        {
          "name": "Elf",
          "size": "Medium",
          "speed": "30 ft",
          "desc": "Elves are a magical people.",
          "traits": "Darkvision, Keen Senses",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "weapons" => """
        {
          "name": "Longsword",
          "category": "Martial Melee",
          "damage_dice": "1d8",
          "damage_type": "slashing",
          "cost": "15 gp",
          "weight": "3 lb.",
          "properties": [ "Versatile", { "name": "Heavy" } ],
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        "armor" => """
        {
          "name": "Chain Mail",
          "category": "Heavy Armor",
          "base_ac": "16",
          "cost": "75 gp",
          "weight": "55 lb.",
          "strength_requirement": "13",
          "stealth_disadvantage": "true",
          "document": { "name": "System Reference Document 5.1" }
        }
        """,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

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
