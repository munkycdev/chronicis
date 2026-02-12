using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Parses wiki-style links from article content using regex pattern matching.
/// Supports both legacy markdown format and modern HTML span format.
/// </summary>
public class LinkParser : ILinkParser
{
    // Legacy regex pattern to match [[guid]] or [[guid|display text]]
    // Guid format: 8-4-4-4-12 hex characters with dashes
    private static readonly Regex LegacyLinkPattern = new(
        @"\[\[([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\|([^\]]+))?\]\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // HTML span pattern for TipTap wiki-link nodes
    // Matches: <span ... data-target-id="guid" ...>Display Text</span>
    // Uses single quotes in the pattern to avoid C# escaping issues
    private static readonly Regex HtmlLinkPattern = new(
        @"<span[^>]+data-target-id=""([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})""[^>]*>([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Extracts all wiki links from the given article body.
    /// Supports both legacy [[guid|text]] format and HTML span format.
    /// </summary>
    /// <param name="body">The article body to parse.</param>
    /// <returns>Collection of parsed links with target ID, display text, and position.</returns>
    public IEnumerable<ParsedLink> ParseLinks(string? body)
    {
        // Return empty if body is null or empty
        if (string.IsNullOrEmpty(body))
        {
            return Enumerable.Empty<ParsedLink>();
        }

        var links = new List<ParsedLink>();
        var processedGuids = new HashSet<Guid>(); // Track unique links

        // Parse HTML span format (TipTap output) - check for marker first
        if (body.Contains("data-target-id="))
        {
            ParseHtmlLinks(body, links, processedGuids);
        }

        // Parse legacy markdown format for backwards compatibility
        if (body.Contains("[["))
        {
            ParseLegacyLinks(body, links, processedGuids);
        }

        return links;
    }

    private static void ParseHtmlLinks(string body, List<ParsedLink> links, HashSet<Guid> processedGuids)
    {
        var matches = HtmlLinkPattern.Matches(body);

        foreach (Match match in matches)
        {
            var guidString = match.Groups[1].Value;

            if (!Guid.TryParse(guidString, out var targetArticleId))
            {
                continue;
            }

            // Skip if we've already processed this target
            if (!processedGuids.Add(targetArticleId))
            {
                continue;
            }

            // Get display text and trim whitespace
            var displayText = match.Groups[2].Success
                ? match.Groups[2].Value.Trim()
                : null;

            var position = match.Index;

            links.Add(new ParsedLink(targetArticleId, displayText, position));
        }
    }

    private static void ParseLegacyLinks(string body, List<ParsedLink> links, HashSet<Guid> processedGuids)
    {
        var matches = LegacyLinkPattern.Matches(body);

        foreach (Match match in matches)
        {
            var guidString = match.Groups[1].Value;

            if (!Guid.TryParse(guidString, out var targetArticleId))
            {
                continue;
            }

            // Skip if we've already processed this target (from HTML parsing)
            if (!processedGuids.Add(targetArticleId))
            {
                continue;
            }

            var displayText = match.Groups[2].Success
                ? match.Groups[2].Value.Trim()
                : null;

            var position = match.Index;

            links.Add(new ParsedLink(targetArticleId, displayText, position));
        }
    }
}
