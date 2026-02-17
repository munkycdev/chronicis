using System.Text.Json.Serialization;

namespace Chronicis.Client.Models;

/// <summary>
/// A visual section grouping related fields in the rendered output.
/// </summary>
public class RenderSection
{
    /// <summary>Section heading displayed to the user.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON field path. When set, this section renders
    /// a nested array/object at that path instead of top-level fields.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// How to render this section's content.
    /// "fields" (default) = key-value pairs.
    /// "list" = array of items, each rendered with itemFields.
    /// "table" = tabular display of array items.
    /// "stat-row" = compact horizontal table (labels on top, values below).
    ///              Ideal for D&amp;D ability scores and similar compact stat groups.
    /// </summary>
    [JsonPropertyName("render")]
    public string Render { get; set; } = "fields";

    /// <summary>Top-level fields to display in this section.</summary>
    [JsonPropertyName("fields")]
    public List<RenderField>? Fields { get; set; }

    /// <summary>
    /// Field definitions for each item when rendering arrays (render = "list" or "table").
    /// </summary>
    [JsonPropertyName("itemFields")]
    public List<RenderField>? ItemFields { get; set; }

    /// <summary>Whether this section starts collapsed. Default false (expanded).</summary>
    [JsonPropertyName("collapsed")]
    public bool Collapsed { get; set; }
}
