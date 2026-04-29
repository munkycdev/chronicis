using System.Text.RegularExpressions;

namespace Chronicis.Shared.Utilities;

/// <summary>
/// Utility class for generating URL-safe slugs from article titles.
/// </summary>
public static class SlugGenerator
{
    private static readonly Regex InvalidCharsRegex = new(@"[^a-z0-9-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphensRegex = new(@"-{2,}", RegexOptions.Compiled);

    /// <summary>
    /// Generates a URL-safe slug from a title.
    /// Rules:
    /// - Converts to lowercase
    /// - Replaces spaces with hyphens
    /// - Removes all characters except a-z, 0-9, and hyphens
    /// - Collapses multiple consecutive hyphens into single hyphen
    /// - Trims hyphens from start and end
    /// - Returns "untitled" if result is empty
    /// </summary>
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "untitled";

        // Convert to lowercase
        var slug = title.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");

        // Remove invalid characters (keep only a-z, 0-9, hyphens)
        slug = InvalidCharsRegex.Replace(slug, string.Empty);

        // Replace multiple consecutive hyphens with single hyphen
        slug = MultipleHyphensRegex.Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    /// <summary>
    /// Validates if a slug is URL-safe.
    /// </summary>
    public static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        // Must be lowercase alphanumeric and hyphens only
        return Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }

    /// <summary>
    /// Generates a unique slug by appending a counter if needed.
    /// </summary>
    public static string GenerateUniqueSlug(string baseSlug, HashSet<string> existingSlugs)
    {
        if (!existingSlugs.Contains(baseSlug))
            return baseSlug;

        var counter = 2;
        string uniqueSlug;
        do
        {
            uniqueSlug = $"{baseSlug}-{counter++}";
        }
        while (existingSlugs.Contains(uniqueSlug));

        return uniqueSlug;
    }

    /// <summary>
    /// Generates a unique slug that is not in <paramref name="existingSiblingSlugs"/> and not in
    /// <paramref name="reservedSlugs"/>. If <paramref name="baseSlug"/> itself is reserved or
    /// already taken, appends <c>-2</c>, <c>-3</c>, … until a free value is found.
    /// </summary>
    public static string GenerateUniqueSiblingSlug(
        string baseSlug,
        HashSet<string> existingSiblingSlugs,
        IReadOnlyCollection<string> reservedSlugs)
    {
        // Normalise to a valid slug shape first
        var slug = IsValidSlug(baseSlug) ? baseSlug : GenerateSlug(baseSlug);

        var reserved = new HashSet<string>(reservedSlugs, StringComparer.OrdinalIgnoreCase);

        if (!existingSiblingSlugs.Contains(slug) && !reserved.Contains(slug))
            return slug;

        var counter = 2;
        string candidate;
        do
        {
            candidate = $"{slug}-{counter++}";
        }
        while (existingSiblingSlugs.Contains(candidate) || reserved.Contains(candidate));

        return candidate;
    }
}
