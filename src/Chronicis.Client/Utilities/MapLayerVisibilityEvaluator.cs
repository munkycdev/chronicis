using Chronicis.Shared.DTOs.Maps;

namespace Chronicis.Client.Utilities;

internal static class MapLayerVisibilityEvaluator
{
    public static HashSet<Guid> GetEffectivelyVisibleLayerIds(IReadOnlyCollection<MapLayerDto> layers)
    {
        if (layers.Count == 0)
        {
            return [];
        }

        var layersById = layers.ToDictionary(layer => layer.MapLayerId);
        var visibleLayerIds = new HashSet<Guid>();
        foreach (var layer in layers)
        {
            if (IsEffectivelyVisible(layer, layersById))
            {
                visibleLayerIds.Add(layer.MapLayerId);
            }
        }

        return visibleLayerIds;
    }

    public static bool IsEffectivelyVisible(MapLayerDto layer, IReadOnlyDictionary<Guid, MapLayerDto> layersById)
    {
        if (!layer.IsEnabled)
        {
            return false;
        }

        var visitedLayerIds = new HashSet<Guid> { layer.MapLayerId };
        var currentParentId = layer.ParentLayerId;
        while (currentParentId.HasValue)
        {
            if (!layersById.TryGetValue(currentParentId.Value, out var parentLayer))
            {
                return false;
            }

            if (!visitedLayerIds.Add(currentParentId.Value))
            {
                return false;
            }

            if (!parentLayer.IsEnabled)
            {
                return false;
            }

            currentParentId = parentLayer.ParentLayerId;
        }

        return true;
    }
}
