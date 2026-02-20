using System.Text.Json;

namespace Chronicis.Client.Services;

/// <summary>
/// Shared formatting and classification helpers for render definition generation.
/// </summary>
public static class RenderDefinitionHelpers
{
    private static readonly HashSet<string> HiddenFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "pk", "model", "document", "illustration", "url", "key", "slug",
        "hover", "v2_converted_path", "img_main", "document__slug",
        "document__title", "document__license_url", "document__url",
        "page_no", "spell_list", "environments"
    };

    public static readonly string[] TitleCandidates = { "name", "title" };

    public static readonly string[] AbilitySuffixes =
        { "strength", "dexterity", "constitution", "intelligence", "wisdom", "charisma" };

    public static readonly string[] AbilityLabels =
        { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

    public static bool IsHiddenField(string name) => HiddenFields.Contains(name);

    public static bool IsNullOrEmpty(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ||
                                    value.GetString() == "—" || value.GetString() == "-",
            JsonValueKind.Array => value.GetArrayLength() == 0,
            _ => false
        };
    }

    public static bool IsDescriptionField(string name) =>
        name.Contains("description", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("desc", StringComparison.OrdinalIgnoreCase);

    public static string FormatFieldName(string name)
    {
        return string.Join(' ', name
            .Split('_')
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    }

    public static string FormatGroupLabel(string prefix)
    {
        var label = FormatFieldName(prefix);
        if (!label.EndsWith('s') && !label.EndsWith("es"))
            label += "s";
        return label;
    }

    public static string StripPrefix(string name, string prefix)
    {
        if (name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
            return name[(prefix.Length + 1)..];
        return name;
    }
}
