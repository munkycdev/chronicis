using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Chronicis.Client.Models;

/// <summary>
/// Defines how external link JSON content should be rendered for a given category.
/// Loaded from wwwroot/render-definitions/{category}.json.
/// </summary>
[ExcludeFromCodeCoverage]
public class RenderDefinition
{
    /// <summary>Schema version for forward compatibility.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>Human-readable name for this content type (e.g., "Bestiary Entry").</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Field path within 'fields' to use as the title. Defaults to "name".</summary>
    [JsonPropertyName("titleField")]
    public string TitleField { get; set; } = "name";

    /// <summary>Ordered sections to render. Each section groups related fields.</summary>
    [JsonPropertyName("sections")]
    public List<RenderSection> Sections { get; set; } = new();

    /// <summary>Field names to hide globally (e.g., "pk", "model").</summary>
    [JsonPropertyName("hidden")]
    public List<string> Hidden { get; set; } = new();

    /// <summary>
    /// When true, fields not mentioned in any section are rendered
    /// in an "Other" catch-all section. Prevents data loss.
    /// </summary>
    [JsonPropertyName("catchAll")]
    public bool CatchAll { get; set; } = true;
}
