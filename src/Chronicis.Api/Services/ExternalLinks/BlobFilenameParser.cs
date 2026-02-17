using System.Text;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Parses blob filenames to derive slugs and titles.
/// Handles SRD filename conventions (e.g., "srd-2024_animated-armor.json").
/// </summary>
public static partial class BlobFilenameParser
{
    // Matches any sequence of non-alphanumeric characters (to replace with single hyphen)
    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonSlugCharsPattern();

    /// <summary>
    /// Derives a slug from a blob filename.
    /// </summary>
    /// <param name="filename">Full filename including extension (e.g., "srd-2024_animated-armor.json").</param>
    /// <returns>Normalized slug (e.g., "animated-armor"), or empty string if normalization fails.</returns>
    /// <remarks>
    /// Rules:
    /// 1. Remove .json extension
    /// 2. If contains underscore, take substring after first underscore
    /// 3. Otherwise, use entire base filename
    /// 4. Normalize:
    ///    - Convert to lowercase
    ///    - Replace runs of non-alphanumeric chars with single hyphen (preserves word boundaries)
    ///    - Trim leading/trailing hyphens
    ///    - Collapse multiple consecutive hyphens to single hyphen
    /// 5. If result is empty, return empty string (caller should skip and log warning)
    /// </remarks>
    public static string DeriveSlug(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return string.Empty;
        }

        // Remove .json extension if present
        var baseName = filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? filename[..^5]
            : filename;

        // If contains underscore, take substring after first underscore
        var underscoreIndex = baseName.IndexOf('_');
        var slug = underscoreIndex >= 0
            ? baseName[(underscoreIndex + 1)..]
            : baseName;

        // Normalize slug: preserve word boundaries by replacing non-alphanumeric runs with hyphens
        slug = NormalizeSlug(slug);

        // Guard: If normalization produced empty slug, try fallback with full base name
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = NormalizeSlug(baseName);

            // If still empty after fallback, return empty (caller MUST skip and log warning)
            if (string.IsNullOrWhiteSpace(slug))
            {
                return string.Empty;
            }
        }

        return slug;
    }

    /// <summary>
    /// Normalizes a string to a valid slug format.
    /// Preserves word boundaries by replacing non-alphanumeric runs with single hyphens.
    /// </summary>
    private static string NormalizeSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Convert to lowercase
        var normalized = input.ToLowerInvariant();

        // Replace any run of non-alphanumeric characters with a single hyphen
        // This preserves word boundaries: "hello world!" -> "hello-world"
        normalized = NonSlugCharsPattern().Replace(normalized, "-");

        // Trim leading and trailing hyphens
        normalized = normalized.Trim('-');

        // Collapse multiple consecutive hyphens to single hyphen
        // (shouldn't happen with regex above, but defensive)
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-");
        }

        return normalized;
    }

    /// <summary>
    /// Prettifies a slug for display as a title.
    /// Culture-invariant and deterministic.
    /// </summary>
    /// <param name="slug">Slug to prettify (e.g., "animated-armor").</param>
    /// <returns>Human-readable title (e.g., "Animated Armor").</returns>
    public static string PrettifySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        // Replace hyphens with spaces and title case each word (culture-invariant)
        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var word in words)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            // Title case: first letter uppercase (invariant), rest lowercase
            if (word.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    sb.Append(word[1..].ToLowerInvariant());
                }
            }
        }

        return sb.ToString();
    }
}
