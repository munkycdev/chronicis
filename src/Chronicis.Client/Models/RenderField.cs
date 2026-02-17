using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicis.Client.Models;

/// <summary>
/// Defines how a single JSON field should be rendered.
/// </summary>
[ExcludeFromCodeCoverage]
public class RenderField
{
    /// <summary>
    /// JSON field name(s) relative to the current context (typically within 'fields').
    /// Can be a single string or an array of strings. When multiple paths are provided,
    /// their values are concatenated (space-separated) for display.
    /// </summary>
    [JsonPropertyName("path")]
    [JsonConverter(typeof(StringOrStringArrayConverter))]
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// Convenience property for single-path fields.
    /// Returns the first path, or empty string if none.
    /// </summary>
    [JsonIgnore]
    public string Path
    {
        get => Paths.Count > 0 ? Paths[0] : string.Empty;
        set => Paths = new List<string> { value };
    }

    /// <summary>Display label override. If null, derived from the field name.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Render hint for this field:
    /// "text" (default) = plain inline value.
    /// "richtext" = rendered as markdown/HTML block.
    /// "heading" = rendered as a sub-heading.
    /// "chips" = array values rendered as tag chips.
    /// "hidden" = not displayed.
    /// </summary>
    [JsonPropertyName("render")]
    public string Render { get; set; } = "text";

    /// <summary>
    /// If true (default), fields with null/empty values are omitted from rendering.
    /// Set to false to always show the field even when empty.
    /// </summary>
    [JsonPropertyName("omitNull")]
    public bool OmitNull { get; set; } = true;
}

/// <summary>
/// Deserializes either a JSON string or a JSON array of strings into List&lt;string&gt;.
/// Serializes as a single string when the list has exactly one element.
/// </summary>
[ExcludeFromCodeCoverage]
public class StringOrStringArrayConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new List<string> { reader.GetString() ?? "" };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(reader.GetString() ?? "");
            }
            return list;
        }

        throw new JsonException($"Expected string or array for path, got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
        }
        else
        {
            writer.WriteStartArray();
            foreach (var s in value)
                writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
    }
}
