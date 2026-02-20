using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Defines category-specific rendering and metadata for an Open5e content type.
/// </summary>
public interface IOpen5eCategoryStrategy
{
    /// <summary>Category key used in IDs and routing (e.g., "spells", "monsters").</summary>
    string CategoryKey { get; }

    /// <summary>API endpoint path segment (e.g., "spells", "creatures").</summary>
    string Endpoint { get; }

    /// <summary>Open5e document/gamesystem slug for filtering (e.g., "5e-2014").</summary>
    string DocumentSlug { get; }

    /// <summary>Human-readable display name (e.g., "Spell", "Monster").</summary>
    string DisplayName { get; }

    /// <summary>Emoji icon for the category.</summary>
    string? Icon { get; }

    /// <summary>Web category slug for open5e.com URLs. Defaults to CategoryKey.</summary>
    string WebCategory { get; }

    /// <summary>
    /// Builds markdown content for an item detail page.
    /// </summary>
    string BuildMarkdown(JsonElement root, string title);

    /// <summary>
    /// Builds a subtitle string for search result display.
    /// </summary>
    string BuildSubtitle(JsonElement item);
}
