using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Pages.Maps;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class MapDetailTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IUserApiService _userApi = Substitute.For<IUserApiService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();

    public MapDetailTests()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("test-user");

        Services.AddSingleton(_mapApi);
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_userApi);
        Services.AddSingleton(_articleApi);
        Services.AddSingleton(_treeState);

        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.Arg<Guid>();
            return new WorldDetailDto
            {
                Id = worldId,
                OwnerId = UserId,
                Name = "World",
                Campaigns = []
            };
        });

        _userApi.GetUserProfileAsync().Returns(new UserProfileDto { Id = UserId, DisplayName = "Owner", Email = "owner@test.dev" });
        _mapApi.GetMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(call =>
        {
            var worldId = call.ArgAt<Guid>(0);
            var mapId = call.ArgAt<Guid>(1);
            return (new MapDto
            {
                WorldId = worldId,
                WorldMapId = mapId,
                Name = "Map",
                HasBasemap = true
            }, 200, (string?)null);
        });
        _mapApi.GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns((new GetBasemapReadUrlResponseDto
        {
            ReadUrl = "https://example.test/map.png"
        }, 200, (string?)null));
        _mapApi.GetLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(_ => CreateLayers());
        _mapApi.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(new List<MapPinResponseDto>
        {
            new()
            {
                PinId = Guid.NewGuid(),
                MapId = Guid.NewGuid(),
                LayerId = VisibleChildLayerId,
                Name = "Pin",
                X = 0.6f,
                Y = 0.6f
            }
        });
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(new List<MapFeatureDto>());
    }

    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid RootLayerId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid VisibleChildLayerId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid HiddenParentLayerId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid HiddenChildLayerId = Guid.Parse("20000000-0000-0000-0000-000000000004");

    [Fact]
    public void MapDetail_PersistedPolygon_RendersSvgOverlay()
    {
        var featureId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(featureId, VisibleChildLayerId)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            var path = cut.Find($"path[data-feature-id='{featureId}']");
            Assert.Equal("M 0.1 0.2 L 0.8 0.2 L 0.4 0.7 L 0.1 0.2 Z", path.GetAttribute("d"));
        });
    }

    [Fact]
    public void MapDetail_DisabledAncestor_HidesPolygonAndLeavesPinsIntact()
    {
        var hiddenFeatureId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var visibleFeatureId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(hiddenFeatureId, HiddenChildLayerId),
            CreatePolygonFeature(visibleFeatureId, VisibleChildLayerId)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("path[data-feature-id]"));
            Assert.NotNull(cut.Find($"path[data-feature-id='{visibleFeatureId}']"));
            Assert.DoesNotContain(hiddenFeatureId.ToString(), cut.Markup, StringComparison.Ordinal);
            Assert.Single(cut.FindAll(".map-page__pin"));
        });
    }

    [Fact]
    public void MapDetail_InvalidPersistedPolygon_IsSkipped()
    {
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(Guid.Parse("30000000-0000-0000-0000-000000000004"), VisibleChildLayerId, closed: false)
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("path[data-feature-id]"));
            Assert.Single(cut.FindAll(".map-page__pin"));
        });
    }

    [Fact]
    public void MapDetail_RenderedPolygon_UsesFeatureIdForIdentity()
    {
        var featureA = Guid.Parse("30000000-0000-0000-0000-000000000005");
        var featureB = Guid.Parse("30000000-0000-0000-0000-000000000006");
        _mapApi.ListFeaturesForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(
        [
            CreatePolygonFeature(featureB, VisibleChildLayerId, "B"),
            CreatePolygonFeature(featureA, VisibleChildLayerId, "A")
        ]);

        var cut = RenderMapDetail();

        cut.WaitForAssertion(() =>
        {
            var ids = cut.FindAll("path[data-feature-id]").Select(path => path.GetAttribute("data-feature-id")).ToList();
            Assert.Contains(featureA.ToString(), ids);
            Assert.Contains(featureB.ToString(), ids);
        });
    }

    private IRenderedComponent<MapDetail> RenderMapDetail()
    {
        var worldId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var mapId = Guid.Parse("50000000-0000-0000-0000-000000000001");

        return RenderComponent<MapDetail>(parameters => parameters
            .Add(p => p.WorldId, worldId)
            .Add(p => p.MapId, mapId));
    }

    private static List<MapLayerDto> CreateLayers() =>
    [
        new() { MapLayerId = RootLayerId, Name = "World", SortOrder = 0, IsEnabled = true },
        new() { MapLayerId = HiddenParentLayerId, Name = "Campaign", SortOrder = 1, IsEnabled = false },
        new() { MapLayerId = VisibleChildLayerId, ParentLayerId = RootLayerId, Name = "Visible", SortOrder = 0, IsEnabled = true },
        new() { MapLayerId = HiddenChildLayerId, ParentLayerId = HiddenParentLayerId, Name = "Hidden Child", SortOrder = 0, IsEnabled = true }
    ];

    private static MapFeatureDto CreatePolygonFeature(Guid featureId, Guid layerId, string? name = null, bool closed = true) =>
        new()
        {
            FeatureId = featureId,
            MapId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            LayerId = layerId,
            FeatureType = MapFeatureType.Polygon,
            Name = name ?? "Polygon",
            Polygon = new PolygonGeometryDto
            {
                Type = "Polygon",
                Coordinates =
                [
                    closed
                        ? [[0.1f, 0.2f], [0.8f, 0.2f], [0.4f, 0.7f], [0.1f, 0.2f]]
                        : [[0.1f, 0.2f], [0.8f, 0.2f], [0.4f, 0.7f], [0.2f, 0.3f]]
                ]
            }
        };
}
