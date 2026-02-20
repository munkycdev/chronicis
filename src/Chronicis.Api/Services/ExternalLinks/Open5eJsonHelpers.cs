using System.Text;
using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Shared JSON extraction helpers for Open5e data.
/// All methods are safe against missing/mistyped properties and return null/empty on failure.
/// </summary>
public static class Open5eJsonHelpers
{
    public static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return null;
    }

    public static int? GetInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }
        }
        return null;
    }

    public static string? GetStringFromObject(JsonElement element, string propertyName, string childPropertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            return GetString(value, childPropertyName);
        }
        return null;
    }

    public static bool? GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
                return true;
            if (value.ValueKind == JsonValueKind.False)
                return false;
            if (value.ValueKind == JsonValueKind.String)
            {
                var str = value.GetString()!.ToLowerInvariant();
                return str == "true" || str == "yes";
            }
        }
        return null;
    }

    public static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        var list = new List<string>();
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
            return list;

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                string? str = null;
                if (item.ValueKind == JsonValueKind.String)
                {
                    str = item.GetString();
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    str = GetString(item, "name");
                }

                if (!string.IsNullOrWhiteSpace(str))
                {
                    list.Add(str);
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            var str = value.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                list.Add(str);
            }
        }

        return list;
    }

    public static string? GetSpeedString(JsonElement root)
    {
        if (root.TryGetProperty("speed", out var speed))
        {
            if (speed.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var prop in speed.EnumerateObject())
                {
                    var value = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                    parts.Add($"{prop.Name} {value}");
                }
                return string.Join(", ", parts);
            }
            return speed.ToString();
        }
        return null;
    }

    public static void AppendNamedArray(StringBuilder sb, JsonElement root, string field, string header)
    {
        if (!root.TryGetProperty(field, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        var items = array.EnumerateArray().ToList();
        if (items.Count == 0)
            return;

        sb.AppendLine($"## {header}");
        foreach (var item in items)
        {
            var name = GetString(item, "name");
            var desc = GetString(item, "desc");

            if (!string.IsNullOrEmpty(name))
            {
                sb.AppendLine($"### {name}");
            }
            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine(desc);
            }
            sb.AppendLine();
        }
    }

    public static string BuildAttribution(JsonElement root)
    {
        var docTitle = GetStringFromObject(root, "document", "name")
            ?? GetString(root, "document__title")
            ?? "System Reference Document 5.1";

        return $"Source: {docTitle}";
    }
}
