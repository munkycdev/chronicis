using System.Text.Json;

namespace Chronicis.ResourceCompiler.Serialization;

public static class JsonPathAccessor
{
    public static bool TryGetByPath(JsonElement element, string path, out JsonElement value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split('.', StringSplitOptions.None);
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment)))
        {
            return false;
        }

        var current = element;
        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!current.TryGetProperty(segment, out var next))
            {
                return false;
            }

            current = next;
        }

        value = current;
        return true;
    }

    public static string ToJsonPath(int rowIndex, string fieldPath)
    {
        if (string.IsNullOrWhiteSpace(fieldPath))
        {
            return $"$[{rowIndex}]";
        }

        return $"$[{rowIndex}].{fieldPath}";
    }
}
