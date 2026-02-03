using System.Text.RegularExpressions;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Validates and parses blob-based external link IDs.
/// Security: Prevents path traversal and injection attacks.
/// Supports hierarchical IDs like "items/armor/breastplate" for subcategories.
/// </summary>
public static partial class BlobIdValidator
{
    // ID must be: lowercase alphanumeric + hyphens, one or more slashes
    // Format: "category/slug" OR "category/subcategory/slug"
    // Examples: "spells/fireball", "items/armor/breastplate"
    [GeneratedRegex(@"^[a-z0-9-]+(/[a-z0-9-]+)+$", RegexOptions.Compiled)]
    private static partial Regex ValidIdPattern();

    // Characters that are prohibited in IDs (security)
    private static readonly char[] ProhibitedChars = { '.', '\\', '[', ']', '|' };

    /// <summary>
    /// Validates an external link ID against security rules.
    /// Supports hierarchical paths with multiple slashes (e.g., "items/armor/breastplate").
    /// </summary>
    /// <param name="id">ID to validate.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(string id, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            error = "ID cannot be empty";
            return false;
        }

        // Check for prohibited characters (path traversal, injection attempts)
        if (id.IndexOfAny(ProhibitedChars) >= 0)
        {
            error = "ID contains prohibited characters (. \\ [ ] |)";
            return false;
        }

        // Check for path traversal patterns
        if (id.Contains("..", StringComparison.Ordinal))
        {
            error = "ID contains path traversal pattern (..)";
            return false;
        }

        // Validate format: lowercase alphanumeric + hyphens with at least one slash
        if (!ValidIdPattern().IsMatch(id))
        {
            error = "ID must have format 'category/slug' or 'category/subcategory/slug' with lowercase alphanumeric and hyphens only";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a validated ID into category path and slug components.
    /// For hierarchical IDs, category includes all path segments except the last.
    /// </summary>
    /// <param name="id">ID to parse (e.g., "spells/fireball" or "items/armor/breastplate").</param>
    /// <returns>
    /// Tuple of (categoryPath, slug):
    /// - "spells/fireball" -> ("spells", "fireball")
    /// - "items/armor/breastplate" -> ("items/armor", "breastplate")
    /// Returns (null, null) if invalid format.
    /// </returns>
    public static (string? categoryPath, string? slug) ParseId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return (null, null);
        }

        var lastSlashIndex = id.LastIndexOf('/');
        if (lastSlashIndex <= 0 || lastSlashIndex >= id.Length - 1)
        {
            // No slash, or slash at start/end
            return (null, null);
        }

        var categoryPath = id[..lastSlashIndex];
        var slug = id[(lastSlashIndex + 1)..];

        return (categoryPath, slug);
    }
}
