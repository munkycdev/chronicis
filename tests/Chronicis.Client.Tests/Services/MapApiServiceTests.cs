using System.Net;
using System.Text.Json;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class MapApiServiceTests
{
    private static MapApiService CreateSut(TestHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, NullLogger<MapApiService>.Instance);

    [Fact]
    public async Task ListMapsForWorldAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new MapApiService(http, NullLogger<MapApiService>.Instance);
        var worldId = Guid.NewGuid();

        await sut.ListMapsForWorldAsync(worldId);

        Assert.Contains(calls, c => c == $"GET world/{worldId}/maps");
    }

    [Fact]
    public async Task GetMapAutocompleteAsync_NoQuery_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var sut = CreateSut(handler);

        _ = await sut.GetMapAutocompleteAsync(worldId, null);

        Assert.Contains(calls, c => c == $"GET world/{worldId}/maps/autocomplete");
    }

    [Fact]
    public async Task GetMapAutocompleteAsync_WithQuery_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var sut = CreateSut(handler);

        _ = await sut.GetMapAutocompleteAsync(worldId, "test");

        Assert.Contains(calls, c => c == $"GET world/{worldId}/maps/autocomplete?query=test");
    }

    [Fact]
    public async Task GetMapAutocompleteAsync_DeserializesDtoList()
    {
        var mapId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""[{"mapId":"{{mapId}}","name":"Sword Coast"}]""")
            }));

        var sut = CreateSut(handler);

        var result = await sut.GetMapAutocompleteAsync(worldId, "swo");

        Assert.Single(result);
        Assert.Equal(mapId, result[0].MapId);
        Assert.Equal("Sword Coast", result[0].Name);
    }

    [Fact]
    public async Task GetMapFeatureAutocompleteAsync_WithQuery_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var sut = CreateSut(handler);

        _ = await sut.GetMapFeatureAutocompleteAsync(worldId, "Blackroot Ford");

        Assert.Contains(calls, c => c == $"GET world/{worldId}/maps/features/autocomplete?query=Blackroot%20Ford");
    }

    [Fact]
    public async Task GetMapFeatureAutocompleteAsync_DeserializesDtoList()
    {
        var worldId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""[{"mapFeatureId":"{{featureId}}","mapId":"{{mapId}}","mapName":"Ambria","displayText":"Blackroot Ford"}]""")
            }));

        var sut = CreateSut(handler);

        var result = await sut.GetMapFeatureAutocompleteAsync(worldId, "blackroot");

        Assert.Single(result);
        Assert.Equal(featureId, result[0].MapFeatureId);
        Assert.Equal(mapId, result[0].MapId);
        Assert.Equal("Ambria", result[0].MapName);
        Assert.Equal("Blackroot Ford", result[0].DisplayText);
    }

    [Fact]
    public async Task GetMapAsync_Success_UsesRouteAndReturnsDto()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal($"world/{worldId}/maps/{mapId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"worldMapId":"{{mapId}}","worldId":"{{worldId}}","name":"Map A","hasBasemap":true}""")
            });
        });

        var sut = CreateSut(handler);

        var result = await sut.GetMapAsync(worldId, mapId);

        Assert.NotNull(result.Map);
        Assert.Equal("Map A", result.Map!.Name);
        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetLayersForMapAsync_UsesExpectedRouteAndDeserializes()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers", req.RequestUri!.PathAndQuery.TrimStart('/'));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""[{"mapLayerId":"{{layerId}}","name":"World","sortOrder":0,"isEnabled":true}]""")
            });
        });

        var sut = CreateSut(handler);
        var result = await sut.GetLayersForMapAsync(worldId, mapId);

        Assert.Single(result);
        Assert.Equal(layerId, result[0].MapLayerId);
        Assert.Equal("World", result[0].Name);
        Assert.Equal(0, result[0].SortOrder);
        Assert.True(result[0].IsEnabled);
    }

    [Fact]
    public async Task CreateLayerAsync_UsesExpectedRouteVerbAndPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        const string layerName = "Cities";

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal("Cities", json.RootElement.GetProperty("name").GetString());
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("parentLayerId").ValueKind);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"mapLayerId":"{{layerId}}","name":"Cities","sortOrder":3,"isEnabled":true}""")
            };
        });

        var sut = CreateSut(handler);
        var result = await sut.CreateLayerAsync(worldId, mapId, layerName);

        Assert.Equal(layerId, result.MapLayerId);
        Assert.Equal("Cities", result.Name);
        Assert.Equal(3, result.SortOrder);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task CreateLayerAsync_WithParentId_SendsParentLayerIdInPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(parentLayerId, json.RootElement.GetProperty("parentLayerId").GetGuid());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"mapLayerId":"00000000-0000-0000-0000-000000000001","name":"Child","sortOrder":0,"isEnabled":true}""")
            };
        });

        var sut = CreateSut(handler);
        _ = await sut.CreateLayerAsync(worldId, mapId, "Child", parentLayerId);
    }

    [Fact]
    public async Task CreateLayerAsync_WhenResponseBodyMissing_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            })));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateLayerAsync(Guid.NewGuid(), Guid.NewGuid(), "Cities"));
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_NotFound_ReturnsStatusAndError()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal($"world/{worldId}/maps/{mapId}/basemap", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"error":"Basemap not found for this map"}""")
            });
        });

        var sut = CreateSut(handler);

        var result = await sut.GetBasemapReadUrlAsync(worldId, mapId);

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Basemap not found for this map", result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_InvalidJsonError_ReturnsNullError()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{invalid-json")
            })));

        var result = await sut.GetBasemapReadUrlAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_NonJsonError_ReturnsNullError()
    {
        var content = new StringContent("plain text");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = content
            })));

        var result = await sut.GetBasemapReadUrlAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Basemap);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetBasemapReadUrlAsync_Success_ReturnsDto()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"readUrl":"https://blob.example.com/read"}""")
            }));

        var sut = CreateSut(handler);

        var result = await sut.GetBasemapReadUrlAsync(worldId, mapId);

        Assert.NotNull(result.Basemap);
        Assert.Equal("https://blob.example.com/read", result.Basemap!.ReadUrl);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetMapAsync_WhenHttpThrows_ReturnsNullAndError()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) => throw new HttpRequestException("network")));

        var result = await sut.GetMapAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result.Map);
        Assert.Null(result.StatusCode);
        Assert.Equal("network", result.Error);
    }

    [Fact]
    public async Task UpdateMapAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"worldMapId":"{{mapId}}","worldId":"{{worldId}}","name":"Renamed","hasBasemap":true}""")
            });
        });

        var sut = CreateSut(handler);
        var result = await sut.UpdateMapAsync(worldId, mapId, new MapUpdateDto { Name = "Renamed" });

        Assert.NotNull(result);
        Assert.Equal("Renamed", result!.Name);
        Assert.Contains(calls, c => c == $"PUT world/{worldId}/maps/{mapId}");
    }

    [Fact]
    public async Task UpdateLayerVisibilityAsync_UsesExpectedRouteVerbAndPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers/{layerId}", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("isEnabled", out var isEnabled));
            Assert.True(isEnabled.GetBoolean());

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = CreateSut(handler);

        await sut.UpdateLayerVisibilityAsync(worldId, mapId, layerId, true);
    }

    [Fact]
    public async Task ReorderLayersAsync_UsesExpectedRouteVerbAndOrderedPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var firstLayerId = Guid.NewGuid();
        var secondLayerId = Guid.NewGuid();
        var thirdLayerId = Guid.NewGuid();
        IList<Guid> layerIds = new List<Guid> { secondLayerId, thirdLayerId, firstLayerId };

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers/reorder", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            var payloadIds = json.RootElement
                .GetProperty("layerIds")
                .EnumerateArray()
                .Select(element => element.GetGuid())
                .ToList();

            Assert.Equal(layerIds, payloadIds);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = CreateSut(handler);

        await sut.ReorderLayersAsync(worldId, mapId, layerIds);
    }

    [Fact]
    public async Task RenameLayerAsync_UsesExpectedRouteVerbAndPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers/{layerId}/rename", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal("Settlements", json.RootElement.GetProperty("name").GetString());

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = CreateSut(handler);

        await sut.RenameLayerAsync(worldId, mapId, layerId, "Settlements");
    }

    [Fact]
    public async Task DeleteLayerAsync_UsesExpectedRouteAndVerb()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers/{layerId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var sut = CreateSut(handler);

        await sut.DeleteLayerAsync(worldId, mapId, layerId);
    }

    [Fact]
    public async Task SetLayerParentAsync_UsesExpectedRouteVerbAndPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var parentLayerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/layers/{layerId}/parent", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(parentLayerId, json.RootElement.GetProperty("parentLayerId").GetGuid());

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = CreateSut(handler);

        await sut.SetLayerParentAsync(worldId, mapId, layerId, parentLayerId);
    }

    [Fact]
    public async Task SetLayerParentAsync_WhenClearingParent_SendsNullInPayload()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("parentLayerId").ValueKind);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var sut = CreateSut(handler);

        await sut.SetLayerParentAsync(worldId, mapId, layerId, null);
    }

    [Fact]
    public async Task RenameLayerAsync_WhenRequestFails_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RenameLayerAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Settlements"));
    }

    [Fact]
    public async Task DeleteLayerAsync_WhenRequestFails_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteLayerAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task SetLayerParentAsync_WhenRequestFails_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetLayerParentAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreatePinAsync_UsesExpectedRouteAndBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var request = new MapPinCreateDto
        {
            X = 0.25f,
            Y = 0.75f,
            LinkedArticleId = Guid.NewGuid(),
        };

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/pins", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(request.X, (float)json.RootElement.GetProperty("x").GetDouble());
            Assert.Equal(request.Y, (float)json.RootElement.GetProperty("y").GetDouble());
            Assert.Equal(request.LinkedArticleId, json.RootElement.GetProperty("linkedArticleId").GetGuid());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"pinId":"00000000-0000-0000-0000-000000000001"}""")
            };
        });

        var sut = CreateSut(handler);

        _ = await sut.CreatePinAsync(worldId, mapId, request);
    }

    [Fact]
    public async Task CreateFeatureAsync_UsesExpectedRouteAndBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var request = new MapFeatureCreateDto
        {
            FeatureType = Chronicis.Shared.Enums.MapFeatureType.Polygon,
            LayerId = layerId,
            Name = "Region",
            Polygon = new PolygonGeometryDto
            {
                Type = "Polygon",
                Coordinates =
                [
                    [
                        [0.1f, 0.1f],
                        [0.8f, 0.1f],
                        [0.8f, 0.8f],
                        [0.1f, 0.1f],
                    ],
                ],
            },
        };

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/features", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal((int)Chronicis.Shared.Enums.MapFeatureType.Polygon, json.RootElement.GetProperty("featureType").GetInt32());
            Assert.Equal(layerId, json.RootElement.GetProperty("layerId").GetGuid());
            Assert.Equal("Polygon", json.RootElement.GetProperty("polygon").GetProperty("type").GetString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"featureId":"00000000-0000-0000-0000-000000000001","featureType":1}""")
            };
        });

        var sut = CreateSut(handler);

        _ = await sut.CreateFeatureAsync(worldId, mapId, request);
    }

    [Fact]
    public async Task ListPinsForMapAsync_UsesExpectedRouteAndNoBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/pins", req.RequestUri!.PathAndQuery.TrimStart('/'));
            Assert.Null(req.Content);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var sut = CreateSut(handler);

        _ = await sut.ListPinsForMapAsync(worldId, mapId);
    }

    [Fact]
    public async Task ListFeaturesForMapAsync_UsesExpectedRouteAndNoBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/features", req.RequestUri!.PathAndQuery.TrimStart('/'));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        });

        var sut = CreateSut(handler);

        _ = await sut.ListFeaturesForMapAsync(worldId, mapId);
    }

    [Fact]
    public async Task GetFeatureAsync_NotFound_ReturnsStatusAndError()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal($"world/{worldId}/maps/{mapId}/features/{featureId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"error":"Feature not found"}""")
            });
        });

        var sut = CreateSut(handler);

        var result = await sut.GetFeatureAsync(worldId, mapId, featureId);

        Assert.Null(result.Feature);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Feature not found", result.Error);
    }

    [Fact]
    public async Task UpdateFeatureAsync_UsesExpectedRouteAndBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var layerId = Guid.NewGuid();
        var request = new MapFeatureUpdateDto
        {
            LayerId = layerId,
            Name = "Updated Region",
            Polygon = new PolygonGeometryDto
            {
                Type = "Polygon",
                Coordinates =
                [
                    [
                        [0.2f, 0.2f],
                        [0.7f, 0.2f],
                        [0.7f, 0.7f],
                        [0.2f, 0.2f],
                    ],
                ],
            },
        };

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/features/{featureId}", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal("Updated Region", json.RootElement.GetProperty("name").GetString());
            Assert.Equal(layerId, json.RootElement.GetProperty("layerId").GetGuid());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"featureId":"00000000-0000-0000-0000-000000000001","featureType":1}""")
            };
        });

        var sut = CreateSut(handler);

        _ = await sut.UpdateFeatureAsync(worldId, mapId, featureId, request);
    }

    [Fact]
    public async Task DeleteFeatureAsync_UsesExpectedRouteAndNoBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/features/{featureId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var sut = CreateSut(handler);

        Assert.True(await sut.DeleteFeatureAsync(worldId, mapId, featureId));
    }

    [Fact]
    public async Task UpdatePinPositionAsync_UsesExpectedRouteAndBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var pinId = Guid.NewGuid();
        var request = new MapPinPositionUpdateDto { X = 0.4f, Y = 0.6f };

        var handler = new TestHttpMessageHandler(async (req, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Patch, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/pins/{pinId}", req.RequestUri!.PathAndQuery.TrimStart('/'));

            var body = await req.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(request.X, (float)json.RootElement.GetProperty("x").GetDouble());
            Assert.Equal(request.Y, (float)json.RootElement.GetProperty("y").GetDouble());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var sut = CreateSut(handler);

        var success = await sut.UpdatePinPositionAsync(worldId, mapId, pinId, request);
        Assert.True(success);
    }

    [Fact]
    public async Task DeletePinAsync_UsesExpectedRouteAndNoBody()
    {
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var pinId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Equal($"world/{worldId}/maps/{mapId}/pins/{pinId}", req.RequestUri!.PathAndQuery.TrimStart('/'));
            Assert.Null(req.Content);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var sut = CreateSut(handler);

        var success = await sut.DeletePinAsync(worldId, mapId, pinId);
        Assert.True(success);
    }

    [Fact]
    public async Task DeleteMapAsync_UsesExpectedRoute()
    {
        var calls = new List<string>();
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var sut = CreateSut(handler);

        var result = await sut.DeleteMapAsync(worldId, mapId);

        Assert.True(result);
        Assert.Contains(calls, c => c == $"DELETE world/{worldId}/maps/{mapId}");
    }
}
