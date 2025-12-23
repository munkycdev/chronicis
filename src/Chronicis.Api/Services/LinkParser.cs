using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Parses wiki-style links from article markdown using regex pattern matching.
/// </summary>
public class LinkParser : ILinkParser
{
    // Regex pattern to match [[guid]] or [[guid|display text]]
    // Guid format: 8-4-4-4-12 hex characters with dashes
    private static readonly Regex LinkPattern = new(
        @"\[\[([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\|([^\]]+))?\]\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Extracts all wiki links from the given article body.
    /// </summary>
    /// <param name="body">The markdown body to parse.</param>
    /// <returns>Collection of parsed links with target ID, display text, and position.</returns>
    public IEnumerable<ParsedLink> ParseLinks(string? body)
    {
        // Return empty if body is null or empty
        if (string.IsNullOrEmpty(body))
        {
            return Enumerable.Empty<ParsedLink>();
        }

        var links = new List<ParsedLink>();
        var matches = LinkPattern.Matches(body);

        foreach (Match match in matches)
        {
            // Extract GUID from capture group 1
            var guidString = match.Groups[1].Value;
            
            // Try to parse the GUID - skip if invalid
            if (!Guid.TryParse(guidString, out var targetArticleId))
            {
                continue;
            }

            // Extract display text from capture group 2 (may be empty)
            var displayText = match.Groups[2].Success 
                ? match.Groups[2].Value.Trim() 
                : null;

            // Get position in the body
            var position = match.Index;

            links.Add(new ParsedLink(targetArticleId, displayText, position));
        }

        return links;
    }
}
