using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Minimal helper for extracting guaranteed fields from SRD JSON blobs.
/// Only extracts fields guaranteed by the blob schema: pk and fields.name.
/// </summary>
internal static class SrdJsonHelper
{
    /// <summary>
    /// Extracts the title from fields.name, with fallback to prettified slug.
    /// </summary>
    /// <param name="json">Parsed JSON document.</param>
    /// <param name="fallbackSlug">Slug to prettify if fields.name is missing.</param>
    /// <returns>Title string.</returns>
    public static string ExtractTitle(JsonDocument json, string fallbackSlug)
    {
        try
        {
            var root = json.RootElement;
            
            // Try to get fields.name
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

        // Fallback: prettify slug
        return BlobFilenameParser.PrettifySlug(fallbackSlug);
    }

    /// <summary>
    /// Extracts the optional pk field for debugging purposes.
    /// </summary>
    public static string? ExtractPk(JsonDocument json)
    {
        try
        {
            var root = json.RootElement;
            if (root.TryGetProperty("pk", out var pkElement) &&
                pkElement.ValueKind == JsonValueKind.String)
            {
                return pkElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}
