namespace Chronicis.Shared;

/// <summary>
/// Utility class for generating URL-friendly slugs from titles.
/// </summary>
public static class SlugUtility
{
    /// <summary>
    /// Creates a URL-friendly slug from a title.
    /// </summary>
    /// <param name="title">The title to convert to a slug</param>
    /// <returns>A lowercase, hyphenated slug suitable for URLs</returns>
    /// <example>
    /// "Hello World!" becomes "hello-world"
    /// "Session 1: The Beginning" becomes "session-1-the-beginning"
    /// </example>
    public static string CreateSlug(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "untitled";
        }

        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(":", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Trim('-');
    }
}
