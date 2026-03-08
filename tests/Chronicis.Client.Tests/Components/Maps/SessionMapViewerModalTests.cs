using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Maps;

public class SessionMapViewerModalTests : MudBlazorTestContext
{
    private readonly IMapApiService _mapApi = Substitute.For<IMapApiService>();
    private readonly Guid _worldId = Guid.Parse("a00f3f5a-6a8e-4f2a-a16d-4cad56429061");
    private readonly Guid _mapId = Guid.Parse("8bbca211-54d4-4b7e-bf3b-2034b91ff17f");

    public SessionMapViewerModalTests()
    {
        Services.AddSingleton(_mapApi);
        _mapApi.GetLayersForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapLayerDto>());
        _mapApi.ListPinsForMapAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new List<MapPinResponseDto>());
        _mapApi.UpdateLayerVisibilityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Modal_FetchesOnOpen_AndWhenMapChangesWhileOpen()
    {
        var secondMapId = Guid.Parse("f35c6eb5-954a-4f58-a198-f11f489a447a");
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/one" }, 200, null));
        _mapApi.GetBasemapReadUrlAsync(_worldId, secondMapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/two" }, 200, null));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, false)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        _mapApi.DidNotReceive().GetBasemapReadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>());

        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetBasemapReadUrlAsync(_worldId, _mapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetLayersForMapAsync(_worldId, _mapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).ListPinsForMapAsync(_worldId, _mapId));

        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, secondMapId));

        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetBasemapReadUrlAsync(_worldId, secondMapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).GetLayersForMapAsync(_worldId, secondMapId));
        cut.WaitForAssertion(() =>
            _mapApi.Received(1).ListPinsForMapAsync(_worldId, secondMapId));
    }

    [Fact]
    public void Modal_WhenBasemapLoads_RendersImageOnly()
    {
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.MapName, "Sword Coast"));

        cut.WaitForAssertion(() =>
        {
            var image = cut.Find(".session-map-viewer-modal__image");
            Assert.Equal(readUrl, image.GetAttribute("src"));
            Assert.Equal("Sword Coast", image.GetAttribute("alt"));
        });
    }

    [Fact]
    public void Modal_WhenPinsExist_RendersPinsOverlay()
    {
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new()
                {
                    PinId = Guid.Parse("2e2dbf4b-8de8-4684-82b9-10188b3d44a0"),
                    MapId = _mapId,
                    LayerId = Guid.Parse("44b3db3d-ae17-48ee-b31d-b2f93782595b"),
                    Name = "Castle",
                    X = 0.25f,
                    Y = 0.50f
                }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin-name"));
        });
    }

    [Fact]
    public void Modal_LayersRenderAsNestedHierarchy()
    {
        const string readUrl = "https://blob.example.com/read";
        var worldId = Guid.Parse("557f2a83-8645-48e4-86ba-df8619e6c096");
        var citiesId = Guid.Parse("16cb18f7-9d2f-4e17-b530-92f1ac3f5380");
        var terrainId = Guid.Parse("cf99ebef-e9f1-4e4d-a985-9b3fdcd9f152");
        var capitalId = Guid.Parse("60a93f52-db84-4822-9f52-c32fefb5140b");
        var portsId = Guid.Parse("cc5a4657-4af7-4a5c-ab1b-bd3c7b94c728");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = portsId, Name = "Ports", SortOrder = 1, IsEnabled = true, ParentLayerId = citiesId },
                new() { MapLayerId = terrainId, Name = "Terrain", SortOrder = 1, IsEnabled = true, ParentLayerId = worldId },
                new() { MapLayerId = citiesId, Name = "Cities", SortOrder = 0, IsEnabled = true, ParentLayerId = worldId },
                new() { MapLayerId = worldId, Name = "World", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = capitalId, Name = "Capital", SortOrder = 0, IsEnabled = true, ParentLayerId = citiesId }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            var layerRows = cut.FindAll("[data-layer-id]")
                .Select(x => new
                {
                    Id = x.GetAttribute("data-layer-id"),
                    Depth = x.GetAttribute("data-layer-depth"),
                    Name = x.QuerySelector(".session-map-viewer-modal__layer-name")!.TextContent.Trim()
                })
                .ToList();
            Assert.Equal(
                [worldId.ToString(), citiesId.ToString(), capitalId.ToString(), portsId.ToString(), terrainId.ToString()],
                layerRows.Select(row => row.Id).ToList());
            Assert.Equal(["0", "1", "2", "2", "1"], layerRows.Select(row => row.Depth).ToList());
            Assert.Equal(["World", "Cities", "Capital", "Ports", "Terrain"], layerRows.Select(row => row.Name).ToList());
        });
    }

    [Fact]
    public void Modal_OnlyParentRowsRenderDisclosureControls()
    {
        const string readUrl = "https://blob.example.com/read";
        var rootId = Guid.Parse("3c56fef8-7f1c-42b5-b6da-c8af20a1ef3f");
        var childId = Guid.Parse("6fe96fd4-b32d-4f79-b0ea-3e00d48361a8");
        var leafRootId = Guid.Parse("f677af67-bb79-437b-9303-00ee1e27eb69");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = rootId, Name = "Root", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = childId, Name = "Child", SortOrder = 0, IsEnabled = true, ParentLayerId = rootId },
                new() { MapLayerId = leafRootId, Name = "Leaf Root", SortOrder = 1, IsEnabled = true }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            var disclosures = cut.FindAll("[data-layer-disclosure]");
            Assert.Single(disclosures);
            Assert.Equal(rootId.ToString(), disclosures[0].GetAttribute("data-layer-disclosure"));
            Assert.Equal("true", disclosures[0].GetAttribute("aria-expanded"));
            Assert.Empty(cut.FindAll($"[data-layer-id='{childId}'] [data-layer-disclosure]"));
            Assert.Empty(cut.FindAll($"[data-layer-id='{leafRootId}'] [data-layer-disclosure]"));
        });
    }

    [Fact]
    public void Modal_CollapseAndExpand_HidesOnlyDescendantsOfThatBranch()
    {
        const string readUrl = "https://blob.example.com/read";
        var rootId = Guid.Parse("fb1dccd5-c6e5-4116-9db8-b1a6e4cb087d");
        var branchAId = Guid.Parse("0870ad54-60f3-4cfc-94f2-7104ee7c4f44");
        var branchALeafId = Guid.Parse("43b1c972-3144-4d40-82a9-c983f5f87516");
        var branchBId = Guid.Parse("8cab907e-06cf-4935-9e14-00efc34efd3e");
        var branchBLeafId = Guid.Parse("b5b9831c-8006-4417-8183-cccf2a0a9be1");
        var peerRootId = Guid.Parse("9d0de65c-becc-4da1-b48b-36d707177494");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = rootId, Name = "Root", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = branchAId, Name = "Branch A", SortOrder = 0, IsEnabled = true, ParentLayerId = rootId },
                new() { MapLayerId = branchALeafId, Name = "Branch A Leaf", SortOrder = 0, IsEnabled = true, ParentLayerId = branchAId },
                new() { MapLayerId = branchBId, Name = "Branch B", SortOrder = 1, IsEnabled = true, ParentLayerId = rootId },
                new() { MapLayerId = branchBLeafId, Name = "Branch B Leaf", SortOrder = 0, IsEnabled = true, ParentLayerId = branchBId },
                new() { MapLayerId = peerRootId, Name = "Peer Root", SortOrder = 1, IsEnabled = true }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Equal(6, cut.FindAll("[data-layer-id]").Count));

        cut.Find($"[data-layer-disclosure='{branchAId}']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(cut.FindAll("[data-layer-id]").Select(row => row.GetAttribute("data-layer-id")), id => id == branchALeafId.ToString());
            Assert.Contains(cut.FindAll("[data-layer-id]").Select(row => row.GetAttribute("data-layer-id")), id => id == branchBId.ToString());
            Assert.Contains(cut.FindAll("[data-layer-id]").Select(row => row.GetAttribute("data-layer-id")), id => id == branchBLeafId.ToString());
            Assert.Contains(cut.FindAll("[data-layer-id]").Select(row => row.GetAttribute("data-layer-id")), id => id == peerRootId.ToString());
            Assert.Equal("false", cut.Find($"[data-layer-disclosure='{branchAId}']").GetAttribute("aria-expanded"));
        });

        cut.Find($"[data-layer-disclosure='{branchAId}']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(cut.FindAll("[data-layer-id]").Select(row => row.GetAttribute("data-layer-id")), id => id == branchALeafId.ToString());
            Assert.Equal("true", cut.Find($"[data-layer-disclosure='{branchAId}']").GetAttribute("aria-expanded"));
        });
    }

    [Fact]
    public void Modal_ToggleInvokesUpdateLayerVisibility()
    {
        const string readUrl = "https://blob.example.com/read";
        var layerId = Guid.Parse("1fbd29a8-7449-4470-b101-e39f9b51d619");
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = layerId, Name = "Arc", SortOrder = 0, IsEnabled = true }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Single(cut.FindAll(".session-map-viewer-modal__layer-toggle")));

        cut.Find(".session-map-viewer-modal__layer-toggle").Change(false);

        cut.WaitForAssertion(() =>
            _mapApi.Received(1).UpdateLayerVisibilityAsync(_worldId, _mapId, layerId, false));
    }

    [Fact]
    public async Task Modal_InheritedVisibility_HiddenParentHidesAndReenableRestoresChildPin()
    {
        const string readUrl = "https://blob.example.com/read";
        var parentLayerId = Guid.Parse("58977eba-cf26-4ff8-a5f8-970ea28bf8a4");
        var childLayerId = Guid.Parse("6d7f6081-fc5e-440d-935c-a8f2e63f95d0");
        var siblingLayerId = Guid.Parse("042a1feb-4e4a-4008-aea3-c6f920be0b73");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 0, IsEnabled = false },
                new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 1, IsEnabled = true, ParentLayerId = parentLayerId },
                new() { MapLayerId = siblingLayerId, Name = "Sibling", SortOrder = 2, IsEnabled = true }
            });
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new()
                {
                    PinId = Guid.Parse("896d266f-c2ca-4274-96d0-03c0d45f19fd"),
                    MapId = _mapId,
                    LayerId = childLayerId,
                    Name = "Child Pin",
                    X = 0.2f,
                    Y = 0.2f
                },
                new()
                {
                    PinId = Guid.Parse("96cf8fdb-0dd8-4132-a335-b4c1570fcc27"),
                    MapId = _mapId,
                    LayerId = siblingLayerId,
                    Name = "Sibling Pin",
                    X = 0.6f,
                    Y = 0.6f
                }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            var pinNames = cut.FindAll(".session-map-viewer-modal__pin-name")
                .Select(x => x.TextContent.Trim())
                .ToList();
            Assert.Equal(["Sibling Pin"], pinNames);
        });

        await InvokePrivateTask(cut.Instance, "OnLayerToggleChangedAsync", parentLayerId, new ChangeEventArgs { Value = true });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".session-map-viewer-modal__pin").Count);
            var pinNames = cut.FindAll(".session-map-viewer-modal__pin-name")
                .Select(x => x.TextContent.Trim())
                .OrderBy(name => name)
                .ToList();
            Assert.Equal(["Child Pin", "Sibling Pin"], pinNames);
        });

        var layers = (List<MapLayerDto>)GetField(cut.Instance, "_layers")!;
        Assert.True(layers.Single(layer => layer.MapLayerId == childLayerId).IsEnabled);
    }

    [Fact]
    public async Task Modal_InheritedVisibility_ExplicitlyDisabledChildRemainsHidden()
    {
        const string readUrl = "https://blob.example.com/read";
        var parentLayerId = Guid.Parse("dbda4609-e783-47d0-8504-c2e74f14a592");
        var childLayerId = Guid.Parse("8d7e63f6-f4cb-4f7f-a3a1-09197a824d44");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = parentLayerId, Name = "Parent", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = childLayerId, Name = "Child", SortOrder = 1, IsEnabled = false, ParentLayerId = parentLayerId }
            });
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = parentLayerId, Name = "Parent Pin", X = 0.1f, Y = 0.2f },
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = childLayerId, Name = "Child Pin", X = 0.3f, Y = 0.4f },
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            var pinNames = cut.FindAll(".session-map-viewer-modal__pin-name")
                .Select(x => x.TextContent.Trim())
                .ToList();
            Assert.Equal(["Parent Pin"], pinNames);
        });

        await InvokePrivateTask(cut.Instance, "OnLayerToggleChangedAsync", parentLayerId, new ChangeEventArgs { Value = false });
        await InvokePrivateTask(cut.Instance, "OnLayerToggleChangedAsync", parentLayerId, new ChangeEventArgs { Value = true });

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            var pinNames = cut.FindAll(".session-map-viewer-modal__pin-name")
                .Select(x => x.TextContent.Trim())
                .ToList();
            Assert.Equal(["Parent Pin"], pinNames);
        });

        var layers = (List<MapLayerDto>)GetField(cut.Instance, "_layers")!;
        Assert.False(layers.Single(layer => layer.MapLayerId == childLayerId).IsEnabled);
    }

    [Fact]
    public void Modal_InheritedVisibility_DeepAncestorAndSiblingIsolation()
    {
        const string readUrl = "https://blob.example.com/read";
        var rootId = Guid.Parse("4933bbf4-c328-45fd-adba-5b6ef7cf70b0");
        var parentAId = Guid.Parse("ec6050a9-b3de-4c20-8690-cd86f273f996");
        var childAId = Guid.Parse("07887992-cc3c-46a2-9d6a-806302c0d3db");
        var parentBId = Guid.Parse("74a83a6c-c170-45dc-a013-d3ba90d48d79");
        var childBId = Guid.Parse("e3db21d2-7afa-4694-a2e7-12595a2bdd6a");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = rootId, Name = "Root", SortOrder = 0, IsEnabled = true },
                new() { MapLayerId = parentAId, Name = "Parent A", SortOrder = 1, IsEnabled = false, ParentLayerId = rootId },
                new() { MapLayerId = childAId, Name = "Child A", SortOrder = 2, IsEnabled = true, ParentLayerId = parentAId },
                new() { MapLayerId = parentBId, Name = "Parent B", SortOrder = 3, IsEnabled = true, ParentLayerId = rootId },
                new() { MapLayerId = childBId, Name = "Child B", SortOrder = 4, IsEnabled = true, ParentLayerId = parentBId },
            });
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = childAId, Name = "Child A Pin", X = 0.2f, Y = 0.2f },
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = childBId, Name = "Child B Pin", X = 0.6f, Y = 0.6f },
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".session-map-viewer-modal__pin"));
            var pinNames = cut.FindAll(".session-map-viewer-modal__pin-name")
                .Select(x => x.TextContent.Trim())
                .ToList();
            Assert.Equal(["Child B Pin"], pinNames);
        });
    }

    [Fact]
    public void Modal_InheritedVisibility_MissingParentAndSelfCycleAreHiddenWithoutThrowing()
    {
        const string readUrl = "https://blob.example.com/read";
        var orphanLayerId = Guid.Parse("c66197d6-9750-41f0-866f-66bc74cd0c00");
        var selfCycleLayerId = Guid.Parse("3ce8dca2-cbc0-4de8-b42d-5151908b0b53");

        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = orphanLayerId, Name = "Orphan", SortOrder = 0, IsEnabled = true, ParentLayerId = Guid.NewGuid() },
                new() { MapLayerId = selfCycleLayerId, Name = "Cycle", SortOrder = 1, IsEnabled = true, ParentLayerId = selfCycleLayerId },
            });
        _mapApi.ListPinsForMapAsync(_worldId, _mapId)
            .Returns(new List<MapPinResponseDto>
            {
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = orphanLayerId, Name = "Orphan Pin", X = 0.2f, Y = 0.2f },
                new() { PinId = Guid.NewGuid(), MapId = _mapId, LayerId = selfCycleLayerId, Name = "Cycle Pin", X = 0.6f, Y = 0.6f },
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".session-map-viewer-modal__pin"));
            Assert.Empty(cut.FindAll(".session-map-viewer-modal__pin-name"));
        });
    }

    [Fact]
    public void Modal_DoesNotRenderManagementControls()
    {
        const string readUrl = "https://blob.example.com/read";
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = readUrl }, 200, null));
        _mapApi.GetLayersForMapAsync(_worldId, _mapId)
            .Returns(new List<MapLayerDto>
            {
                new() { MapLayerId = Guid.NewGuid(), Name = "World", SortOrder = 0, IsEnabled = true }
            });

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.DoesNotContain("Add Root-Level Layer", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Add Child Layer", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rename", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Delete", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Nest", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(">Root<", markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Modal_WhileFetching_RendersLoadingState()
    {
        var tcs = new TaskCompletionSource<(GetBasemapReadUrlResponseDto? Basemap, int? StatusCode, string? Error)>();
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId).Returns(tcs.Task);

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Contains("Loading map...", cut.Markup, StringComparison.OrdinalIgnoreCase));

        tcs.SetResult((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
    }

    [Theory]
    [InlineData(401, "Unauthorized", "You do not have access to this map.")]
    [InlineData(403, "World not found or access denied", "You do not have access to this map.")]
    [InlineData(404, "Map not found", "Map not found.")]
    [InlineData(404, "Basemap not found for this map", "Basemap is missing for this map.")]
    public void Modal_WhenLoadFails_RendersExpectedErrorMessage(int statusCode, string error, string expectedMessage)
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((null, statusCode, error));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        cut.WaitForAssertion(() =>
            Assert.Contains(expectedMessage, cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Modal_CloseButton_InvokesOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal__close", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal__close").Click();

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Modal_EscapeKey_InvokesOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, closeCount);
    }

    [Fact]
    public void Modal_NonEscapeKey_DoesNotInvokeOnClose()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var closeCount = 0;

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCount++)));

        cut.WaitForAssertion(() => Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.Ordinal));
        cut.Find(".session-map-viewer-modal").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, closeCount);
    }

    [Fact]
    public async Task Modal_PrivatePanZoomAndHelperBranches_AreCovered()
    {
        _mapApi.GetBasemapReadUrlAsync(_worldId, _mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));

        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId)
            .Add(x => x.MapName, "  Realm Map  "));

        cut.WaitForAssertion(() => Assert.Contains("Realm Map", cut.Markup, StringComparison.Ordinal));
        var instance = cut.Instance;

        Assert.Equal("session-map-viewer-modal__viewport", (string)InvokePrivate(instance, "GetMapViewportClass")!);

        SetField(instance, "_isMapDragging", true);
        Assert.Contains("--dragging", (string)InvokePrivate(instance, "GetMapViewportClass")!, StringComparison.Ordinal);

        SetField(instance, "_isMapDragging", false);
        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapMinZoom", 1d);
        SetField(instance, "_mapZoom", 2d);
        Assert.Contains("--pan", (string)InvokePrivate(instance, "GetMapViewportClass")!, StringComparison.Ordinal);

        SetField(instance, "_hasMapViewportLayout", false);
        Assert.Equal(
            "width:100%;height:auto;transform:translate(0px,0px) scale(1);",
            (string)InvokePrivate(instance, "GetMapStageStyle")!);

        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapBaseWidth", 400d);
        SetField(instance, "_mapBaseHeight", 300d);
        SetField(instance, "_mapPanX", 12.345d);
        SetField(instance, "_mapPanY", -8.9d);
        SetField(instance, "_mapZoom", 1.25d);
        var stageStyle = (string)InvokePrivate(instance, "GetMapStageStyle")!;
        Assert.Contains("width:400.00px", stageStyle, StringComparison.Ordinal);
        Assert.Contains("height:300.00px", stageStyle, StringComparison.Ordinal);
        Assert.Contains("translate(12.35px,-8.90px)", stageStyle, StringComparison.Ordinal);
        Assert.Contains("scale(1.2500)", stageStyle, StringComparison.Ordinal);

        Assert.Equal(
            "session-map-viewer-modal__image session-map-viewer-modal__image--stage",
            (string)InvokePrivate(instance, "GetMapImageClass")!);
        SetField(instance, "_hasMapViewportLayout", false);
        Assert.Equal("session-map-viewer-modal__image", (string)InvokePrivate(instance, "GetMapImageClass")!);

        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapViewportWidth", 300d);
        SetField(instance, "_mapViewportHeight", 200d);
        SetField(instance, "_mapBaseWidth", 100d);
        SetField(instance, "_mapBaseHeight", 80d);
        SetField(instance, "_mapMinZoom", 1d);
        SetField(instance, "_mapMaxZoom", 5d);
        SetField(instance, "_mapZoom", 1d);
        SetField(instance, "_mapPanX", 0d);
        SetField(instance, "_mapPanY", 0d);

        SetField(instance, "_hasMapViewportLayout", false);
        await InvokePrivateTask(instance, "OnZoomSliderInput", new ChangeEventArgs { Value = "50" });
        SetField(instance, "_hasMapViewportLayout", true);

        await InvokePrivateTask(instance, "OnZoomSliderInput", new ChangeEventArgs { Value = "bad" });
        Assert.Equal(1d, (double)GetField(instance, "_mapZoom")!);

        await InvokePrivateTask(instance, "OnZoomSliderInput", new ChangeEventArgs { Value = "100" });
        Assert.Equal(5d, Math.Round((double)GetField(instance, "_mapZoom")!, 4));

        SetField(instance, "_mapZoom", 5d);
        Assert.False((bool)InvokePrivate(instance, "CanZoomIn")!);
        InvokePrivate(instance, "ZoomInFromButton");
        Assert.Equal(5d, (double)GetField(instance, "_mapZoom")!);
        SetField(instance, "_mapZoom", 1d);
        Assert.False((bool)InvokePrivate(instance, "CanZoomOut")!);

        InvokePrivate(instance, "ZoomOutFromButton");
        Assert.Equal(1d, (double)GetField(instance, "_mapZoom")!);

        InvokePrivate(instance, "ZoomInFromButton");
        Assert.True((double)GetField(instance, "_mapZoom")! > 1d);

        SetField(instance, "_mapZoom", 3d);
        InvokePrivate(instance, "ZoomOutFromButton");
        Assert.True((double)GetField(instance, "_mapZoom")! < 3d);

        SetField(instance, "_hasMapViewportLayout", false);
        var noWheelZoom = (double)GetField(instance, "_mapZoom")!;
        instance.OnWheelZoom(-120d);
        Assert.Equal(noWheelZoom, (double)GetField(instance, "_mapZoom")!);

        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapZoom", 2d);
        instance.OnWheelZoom(-120d);
        var wheelZoom = (double)GetField(instance, "_mapZoom")!;
        instance.OnWheelZoom(120d);
        Assert.True((double)GetField(instance, "_mapZoom")! < wheelZoom);

        SetField(instance, "_mapZoom", 1d);
        InvokePrivate(instance, "OnMapViewportMouseDown", new MouseEventArgs { Button = 0, ClientX = 10d, ClientY = 10d });
        Assert.False((bool)GetField(instance, "_isMapPointerDown")!);

        SetField(instance, "_mapZoom", 2d);
        InvokePrivate(instance, "OnMapViewportMouseDown", new MouseEventArgs { Button = 1, ClientX = 10d, ClientY = 10d });
        Assert.False((bool)GetField(instance, "_isMapPointerDown")!);

        InvokePrivate(instance, "OnMapViewportMouseDown", new MouseEventArgs { Button = 0, ClientX = 10d, ClientY = 10d });
        Assert.True((bool)GetField(instance, "_isMapPointerDown")!);

        InvokePrivate(instance, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 11d, ClientY = 11d });
        Assert.False((bool)GetField(instance, "_isMapDragging")!);

        InvokePrivate(instance, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 20d, ClientY = 18d });
        Assert.True((bool)GetField(instance, "_isMapDragging")!);

        InvokePrivate(instance, "OnMapViewportMouseUp", new MouseEventArgs());
        Assert.False((bool)GetField(instance, "_isMapPointerDown")!);
        Assert.False((bool)GetField(instance, "_isMapDragging")!);

        SetField(instance, "_isMapPointerDown", false);
        var panBeforeNoPointerMove = (double)GetField(instance, "_mapPanX")!;
        InvokePrivate(instance, "OnMapViewportMouseMove", new MouseEventArgs { ClientX = 40d, ClientY = 40d });
        Assert.Equal(panBeforeNoPointerMove, (double)GetField(instance, "_mapPanX")!);

        SetField(instance, "_hasMapViewportLayout", false);
        InvokePrivate(instance, "SetZoomLevel", 4d);

        SetField(instance, "_hasMapViewportLayout", true);
        SetField(instance, "_mapZoom", 2d);
        SetField(instance, "_mapMinZoom", 1d);
        SetField(instance, "_mapMaxZoom", 5d);
        SetField(instance, "_mapViewportWidth", 300d);
        SetField(instance, "_mapViewportHeight", 200d);
        SetField(instance, "_mapBaseWidth", 100d);
        SetField(instance, "_mapBaseHeight", 80d);
        SetField(instance, "_mapPanX", 0d);
        SetField(instance, "_mapPanY", 0d);
        InvokePrivate(instance, "SetZoomLevel", 2d);
        InvokePrivate(instance, "SetZoomLevel", 4d);
        Assert.Equal(4d, Math.Round((double)GetField(instance, "_mapZoom")!, 4));

        InvokePrivate(instance, "RecenterMapPan");
        InvokePrivate(instance, "ClampPanToBounds");
        InvokePrivate(instance, "ResetMapViewportState");
        Assert.False((bool)GetField(instance, "_hasMapViewportLayout")!);
        Assert.Equal(1d, (double)GetField(instance, "_mapZoom")!);

        Assert.False((bool)InvokeStatic("HasUsableBounds", (object?)null)!);
        Assert.True((bool)InvokeStatic("HasUsableBounds", CreateMapElementRect(100d, 50d))!);
        Assert.Equal(0d, (double)InvokeStatic("ClampPanAxis", 10d, 0d, 100d)!);
        Assert.Equal(20d, (double)InvokeStatic("ClampPanAxis", 0d, 100d, 60d)!);
        Assert.Equal(-50d, (double)InvokeStatic("ClampPanAxis", -80d, 50d, 100d)!);
        Assert.Equal(0, (int)InvokeStatic("GetSliderValueForZoom", 1d, 2d, 2d)!);
        Assert.Equal(50, (int)InvokeStatic("GetSliderValueForZoom", 3d, 1d, 5d)!);
        Assert.Equal(1d, (double)InvokeStatic("GetZoomForSliderValue", 50d, 1d, 1d)!);
        Assert.Equal(2d, (double)InvokeStatic("GetZoomForSliderValue", 50d, 1d, 3d)!);

        var parseArgs = new object?[] { "12.5", null };
        Assert.True((bool)InvokeStatic("TryReadDouble", parseArgs)!);
        Assert.Equal(12.5d, (double)parseArgs[1]!);
        var parseNullArgs = new object?[] { null, null };
        Assert.False((bool)InvokeStatic("TryReadDouble", parseNullArgs)!);
        var parseFailArgs = new object?[] { "oops", null };
        Assert.False((bool)InvokeStatic("TryReadDouble", parseFailArgs)!);

        var boolArgs = new object?[] { true, null };
        Assert.True((bool)InvokeStatic("TryReadBool", boolArgs)!);
        Assert.True((bool)boolArgs[1]!);

        var boolStringArgs = new object?[] { "false", null };
        Assert.True((bool)InvokeStatic("TryReadBool", boolStringArgs)!);
        Assert.False((bool)boolStringArgs[1]!);

        var boolFailArgs = new object?[] { "not-bool", null };
        Assert.False((bool)InvokeStatic("TryReadBool", boolFailArgs)!);

        var boolNullArgs = new object?[] { null, null };
        Assert.False((bool)InvokeStatic("TryReadBool", boolNullArgs)!);

        Assert.Equal(0d, (double)InvokeStatic("Clamp", -1d, 0d, 10d)!);
        Assert.Equal(5d, (double)InvokeStatic("Clamp", 5d, 0d, 10d)!);
        Assert.Equal(10d, (double)InvokeStatic("Clamp", 11d, 0d, 10d)!);

        var rootId = Guid.Parse("6dce0116-7d14-4547-ab00-f580f27eb416");
        var childId = Guid.Parse("5691b07a-5f2c-42f4-a786-f637f2451f80");
        var orphanId = Guid.Parse("b2b9d7a6-3d65-4062-a760-6f70f1d4ec47");
        var layers = new List<MapLayerDto>
        {
            new() { MapLayerId = childId, Name = "Child", SortOrder = 0, IsEnabled = true, ParentLayerId = rootId },
            new() { MapLayerId = rootId, Name = "Root", SortOrder = 0, IsEnabled = true },
            new() { MapLayerId = orphanId, Name = "Orphan", SortOrder = 0, IsEnabled = true, ParentLayerId = Guid.NewGuid() }
        };
        SetField(instance, "_layers", layers);

        var orderedSiblingNames = ((IEnumerable<MapLayerDto>)InvokeStatic("OrderSiblingLayers", new[]
        {
            new MapLayerDto { MapLayerId = Guid.Parse("9979ac5a-5434-4012-bd99-b0ba55f73df7"), Name = "B", SortOrder = 1 },
            new MapLayerDto { MapLayerId = Guid.Parse("8ace2b95-92cb-4a33-8024-4f8c853cb22d"), Name = "A", SortOrder = 1 },
            new MapLayerDto { MapLayerId = Guid.Parse("1cb96547-4982-4c5a-bf5a-57f4450031e6"), Name = "C", SortOrder = 0 }
        })!)
            .Select(layer => layer.Name)
            .ToList();
        Assert.Equal(["C", "A", "B"], orderedSiblingNames);

        Assert.True((bool)InvokePrivate(instance, "IsRootLayer", layers.Single(layer => layer.MapLayerId == rootId))!);
        Assert.True((bool)InvokePrivate(instance, "IsRootLayer", layers.Single(layer => layer.MapLayerId == orphanId))!);
        Assert.False((bool)InvokePrivate(instance, "IsRootLayer", layers.Single(layer => layer.MapLayerId == childId))!);

        Assert.Equal("width:0px;", (string)InvokeStatic("GetLayerDepthStyle", -1)!);
        Assert.Equal("width:28px;", (string)InvokeStatic("GetLayerDepthStyle", 2)!);

        var visibleRows = (IEnumerable<object>)InvokePrivate(instance, "GetVisibleLayerRows")!;
        Assert.Equal(3, visibleRows.Count());

        var disclosureLabel = (string)InvokeStatic(
            "GetLayerDisclosureLabel",
            CreateLayerRenderRow(layers.Single(layer => layer.MapLayerId == rootId), depth: 0, hasChildren: true, isCollapsed: false))!;
        Assert.Equal("Collapse Root", disclosureLabel);
        var expandLabel = (string)InvokeStatic(
            "GetLayerDisclosureLabel",
            CreateLayerRenderRow(layers.Single(layer => layer.MapLayerId == rootId), depth: 0, hasChildren: true, isCollapsed: true))!;
        Assert.Equal("Expand Root", expandLabel);

        var collapsedLayerIds = (HashSet<Guid>)GetField(instance, "_collapsedLayerIds")!;
        collapsedLayerIds.Add(rootId);
        InvokePrivate(instance, "ToggleLayerCollapsed", rootId);
        Assert.DoesNotContain(rootId, collapsedLayerIds);
        InvokePrivate(instance, "ToggleLayerCollapsed", childId);
        Assert.DoesNotContain(childId, collapsedLayerIds);
        collapsedLayerIds.Add(Guid.NewGuid());
        InvokePrivate(instance, "PruneCollapsedLayerIds");
        Assert.DoesNotContain(collapsedLayerIds, layerId => !layers.Any(layer => layer.MapLayerId == layerId));

        var pinWithName = new MapPinResponseDto { Name = "Castle", X = 0.25f, Y = 0.5f };
        Assert.Equal("left:25.0000%;top:50.0000%;", (string)InvokeStatic("GetPinStyle", pinWithName)!);
        Assert.Equal("left:25.0000%;top:50.0000%;", (string)InvokeStatic("GetPinLabelStyle", pinWithName)!);
        Assert.True((bool)InvokeStatic("HasPinLabel", pinWithName)!);
        Assert.Equal("Castle", (string)InvokeStatic("GetPinTooltip", pinWithName)!);

        var pinWithArticle = new MapPinResponseDto
        {
            Name = " ",
            LinkedArticle = new LinkedArticleSummaryDto { ArticleId = Guid.NewGuid(), Title = "Linked" }
        };
        Assert.Equal("Linked", (string)InvokeStatic("GetPinTooltip", pinWithArticle)!);

        var pinWithoutName = new MapPinResponseDto { Name = " " };
        Assert.False((bool)InvokeStatic("HasPinLabel", pinWithoutName)!);
        Assert.Equal("Map pin", (string)InvokeStatic("GetPinTooltip", pinWithoutName)!);

        await InvokePrivateTask(instance, "CloseAsync");
    }

    [Fact]
    public async Task Modal_DisposeAsync_WithDisconnectedModule_DoesNotThrow()
    {
        var cut = RenderComponent<SessionMapViewerModal>(parameters => parameters
            .Add(x => x.IsOpen, false)
            .Add(x => x.WorldId, _worldId)
            .Add(x => x.MapId, _mapId));

        var instance = cut.Instance;
        var fakeModule = new FakeJsObjectReference(throwDisconnectedOnDispose: true);
        SetField(instance, "_mapInteropModule", fakeModule);

        await instance.DisposeAsync();

        Assert.True(fakeModule.DisposeCalled);
    }

    private static object? InvokePrivate(object instance, string methodName, params object?[]? args)
        => instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(instance, args);

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static object? InvokeStatic(string methodName, params object?[]? args)
        => typeof(SessionMapViewerModal).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args);

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static object CreateMapElementRect(double width, double height, double left = 0d, double top = 0d)
    {
        var rectType = typeof(SessionMapViewerModal).GetNestedType("MapElementRect", BindingFlags.NonPublic);
        Assert.NotNull(rectType);

        var rect = Activator.CreateInstance(rectType!);
        Assert.NotNull(rect);

        rectType.GetProperty("Left", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(rect, left);
        rectType.GetProperty("Top", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(rect, top);
        rectType.GetProperty("Width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(rect, width);
        rectType.GetProperty("Height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(rect, height);
        return rect;
    }

    private static object CreateLayerRenderRow(MapLayerDto layer, int depth, bool hasChildren, bool isCollapsed)
    {
        var rowType = typeof(SessionMapViewerModal).GetNestedType("LayerRenderRow", BindingFlags.NonPublic);
        Assert.NotNull(rowType);

        var row = Activator.CreateInstance(rowType!, layer, depth, hasChildren, isCollapsed);
        Assert.NotNull(row);
        return row;
    }

    private sealed class FakeJsObjectReference(bool throwDisconnectedOnDispose) : IJSObjectReference
    {
        public bool DisposeCalled { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            DisposeCalled = true;
            if (throwDisconnectedOnDispose)
            {
                throw new JSDisconnectedException("Simulated disconnect");
            }

            return ValueTask.CompletedTask;
        }
    }
}
