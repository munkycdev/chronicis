using System.Text.Json.Serialization;

namespace Chronicis.Client.Models;

/// <summary>
/// Defines how a single JSON field should be rendered.
/// </summary>
public class RenderField
{
    /// <summary>
    /// JSON field name (relative to the current context, typically within 'fields').
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

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
}
