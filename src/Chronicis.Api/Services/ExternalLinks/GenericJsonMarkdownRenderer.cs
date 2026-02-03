using System.Text;
using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Renders JSON content as generic markdown for blob-backed external link providers.
/// Phase 4: Generic rendering without category-specific formatting.
/// </summary>
public static class GenericJsonMarkdownRenderer
{
    /// <summary>
    /// Renders JSON document as markdown.
    /// </summary>
    /// <param name="json">Parsed JSON document.</param>
    /// <param name="displayName">Provider display name for attribution footer.</param>
    /// <param name="fallbackTitle">Fallback title if fields.name is missing.</param>
    /// <returns>Markdown string.</returns>
    public static string RenderMarkdown(JsonDocument json, string displayName, string fallbackTitle)
    {
        var sb = new StringBuilder();
        var root = json.RootElement;

        // Extract title from fields.name
        var title = ExtractTitle(root, fallbackTitle);
        sb.AppendLine($"# {EscapeMarkdown(title)}");
        sb.AppendLine();

        // Render attributes section (everything except pk and fields.name)
        sb.AppendLine("## Attributes");
        sb.AppendLine();

        foreach (var property in root.EnumerateObject())
        {
            // Skip pk (top-level)
            if (property.Name.Equals("pk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Handle fields object specially
            if (property.Name.Equals("fields", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                RenderFieldsObject(sb, property.Value, 0);
            }
            else
            {
                RenderProperty(sb, property.Name, property.Value, 0);
            }
        }

        // Attribution footer
        sb.AppendLine();
        sb.AppendLine($"*Source: {EscapeMarkdown(displayName)}*");

        return sb.ToString();
    }

    private static string ExtractTitle(JsonElement root, string fallback)
    {
        try
        {
            if (root.TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty("name", out var nameElement) &&
                nameElement.ValueKind == JsonValueKind.String)
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }
        catch
        {
            // Parsing failed, use fallback
        }

        return fallback;
    }

    private static void RenderFieldsObject(StringBuilder sb, JsonElement fields, int indent)
    {
        foreach (var property in fields.EnumerateObject())
        {
            // Skip fields.name (already used as title)
            if (property.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RenderProperty(sb, property.Name, property.Value, indent);
        }
    }

    private static void RenderProperty(StringBuilder sb, string key, JsonElement value, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        var displayName = FormatFieldName(key);

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var str = value.GetString() ?? string.Empty;
                sb.AppendLine($"{indentStr}- **{EscapeMarkdown(displayName)}**: {EscapeMarkdown(str)}");
                break;

            case JsonValueKind.Number:
                sb.AppendLine($"{indentStr}- **{EscapeMarkdown(displayName)}**: {value}");
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                sb.AppendLine($"{indentStr}- **{EscapeMarkdown(displayName)}**: {value}");
                break;

            case JsonValueKind.Null:
                sb.AppendLine($"{indentStr}- **{EscapeMarkdown(displayName)}**: null");
                break;

            case JsonValueKind.Array:
                RenderArray(sb, displayName, value, indent);
                break;

            case JsonValueKind.Object:
                sb.AppendLine($"{indentStr}- **{EscapeMarkdown(displayName)}**:");
                RenderObject(sb, value, indent + 1);
                break;

            default:
                // Undefined - skip
                break;
        }
    }

    private static void RenderArray(StringBuilder sb, string key, JsonElement array, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        sb.AppendLine($"{indentStr}- **{EscapeMarkdown(key)}**:");

        var childIndent = indent + 1;
        var childIndentStr = new string(' ', childIndent * 2);

        foreach (var item in array.EnumerateArray())
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.String:
                    var str = item.GetString() ?? string.Empty;
                    sb.AppendLine($"{childIndentStr}- {EscapeMarkdown(str)}");
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    sb.AppendLine($"{childIndentStr}- {item}");
                    break;

                case JsonValueKind.Object:
                    sb.AppendLine($"{childIndentStr}- ");
                    RenderObject(sb, item, childIndent + 1);
                    break;

                case JsonValueKind.Array:
                    // Nested arrays - render inline or recursively (keep simple)
                    sb.AppendLine($"{childIndentStr}- (nested array)");
                    break;

                default:
                    break;
            }
        }
    }

    private static void RenderObject(StringBuilder sb, JsonElement obj, int indent)
    {
        foreach (var property in obj.EnumerateObject())
        {
            RenderProperty(sb, property.Name, property.Value, indent);
        }
    }

    /// <summary>
    /// Formats field names for display: snake_case or camelCase to Title Case.
    /// Examples: armor_class -> Armor Class, spellLevel -> Spell Level
    /// </summary>
    private static string FormatFieldName(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return fieldName;
        }

        // Replace underscores with spaces
        var withSpaces = fieldName.Replace('_', ' ');

        // Insert spaces before capital letters (camelCase handling)
        var sb = new StringBuilder();
        for (int i = 0; i < withSpaces.Length; i++)
        {
            var ch = withSpaces[i];
            
            // Insert space before uppercase if not at start and previous char is lowercase
            if (i > 0 && char.IsUpper(ch) && char.IsLower(withSpaces[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(ch);
        }

        // Convert to title case
        var words = sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleCased = words.Select(w => 
            w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : w);

        return string.Join(" ", titleCased);
    }

    /// <summary>
    /// Escapes markdown special characters to prevent HTML injection.
    /// Minimal escaping: angle brackets to prevent tags.
    /// </summary>
    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
