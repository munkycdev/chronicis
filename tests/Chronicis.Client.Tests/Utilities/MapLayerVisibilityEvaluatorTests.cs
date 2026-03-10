using Chronicis.Client.Utilities;
using Chronicis.Shared.DTOs.Maps;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

public class MapLayerVisibilityEvaluatorTests
{
    [Fact]
    public void GetEffectivelyVisibleLayerIds_ReturnsEmpty_WhenNoLayers()
    {
        var result = MapLayerVisibilityEvaluator.GetEffectivelyVisibleLayerIds([]);

        Assert.Empty(result);
    }

    [Fact]
    public void IsEffectivelyVisible_ReturnsFalse_WhenLayerDisabled()
    {
        var layer = new MapLayerDto { MapLayerId = Guid.NewGuid(), IsEnabled = false };
        var layersById = new Dictionary<Guid, MapLayerDto> { [layer.MapLayerId] = layer };

        var visible = MapLayerVisibilityEvaluator.IsEffectivelyVisible(layer, layersById);

        Assert.False(visible);
    }

    [Fact]
    public void IsEffectivelyVisible_ReturnsFalse_WhenParentMissing()
    {
        var layer = new MapLayerDto
        {
            MapLayerId = Guid.NewGuid(),
            ParentLayerId = Guid.NewGuid(),
            IsEnabled = true
        };

        var layersById = new Dictionary<Guid, MapLayerDto> { [layer.MapLayerId] = layer };

        var visible = MapLayerVisibilityEvaluator.IsEffectivelyVisible(layer, layersById);

        Assert.False(visible);
    }

    [Fact]
    public void IsEffectivelyVisible_ReturnsFalse_WhenAncestorDisabled()
    {
        var parentId = Guid.NewGuid();
        var layer = new MapLayerDto
        {
            MapLayerId = Guid.NewGuid(),
            ParentLayerId = parentId,
            IsEnabled = true
        };

        var parent = new MapLayerDto
        {
            MapLayerId = parentId,
            IsEnabled = false
        };

        var layersById = new Dictionary<Guid, MapLayerDto>
        {
            [layer.MapLayerId] = layer,
            [parent.MapLayerId] = parent
        };

        var visible = MapLayerVisibilityEvaluator.IsEffectivelyVisible(layer, layersById);

        Assert.False(visible);
    }

    [Fact]
    public void IsEffectivelyVisible_ReturnsFalse_WhenParentCycleDetected()
    {
        var layerId = Guid.NewGuid();
        var layer = new MapLayerDto
        {
            MapLayerId = layerId,
            ParentLayerId = layerId,
            IsEnabled = true
        };

        var layersById = new Dictionary<Guid, MapLayerDto> { [layerId] = layer };

        var visible = MapLayerVisibilityEvaluator.IsEffectivelyVisible(layer, layersById);

        Assert.False(visible);
    }

    [Fact]
    public void IsEffectivelyVisible_ReturnsTrue_WhenAncestorsEnabled()
    {
        var rootId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var root = new MapLayerDto { MapLayerId = rootId, IsEnabled = true };
        var parent = new MapLayerDto { MapLayerId = parentId, ParentLayerId = rootId, IsEnabled = true };
        var child = new MapLayerDto { MapLayerId = childId, ParentLayerId = parentId, IsEnabled = true };

        var layersById = new Dictionary<Guid, MapLayerDto>
        {
            [rootId] = root,
            [parentId] = parent,
            [childId] = child
        };

        var visible = MapLayerVisibilityEvaluator.IsEffectivelyVisible(child, layersById);

        Assert.True(visible);
    }

    [Fact]
    public void GetEffectivelyVisibleLayerIds_FiltersToEffectivelyVisibleLayers()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var disabledId = Guid.NewGuid();
        var missingParentChildId = Guid.NewGuid();

        var layers = new List<MapLayerDto>
        {
            new() { MapLayerId = rootId, IsEnabled = true },
            new() { MapLayerId = childId, ParentLayerId = rootId, IsEnabled = true },
            new() { MapLayerId = disabledId, IsEnabled = false },
            new() { MapLayerId = missingParentChildId, ParentLayerId = Guid.NewGuid(), IsEnabled = true }
        };

        var visibleIds = MapLayerVisibilityEvaluator.GetEffectivelyVisibleLayerIds(layers);

        Assert.Contains(rootId, visibleIds);
        Assert.Contains(childId, visibleIds);
        Assert.DoesNotContain(disabledId, visibleIds);
        Assert.DoesNotContain(missingParentChildId, visibleIds);
    }
}
