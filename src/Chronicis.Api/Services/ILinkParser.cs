namespace Chronicis.Api.Services;

/// <summary>
/// Service for parsing wiki-style links from article markdown.
/// </summary>
public interface ILinkParser
{
    /// <summary>
    /// Extracts all wiki links from the given article body.
    /// Format: [[guid]] or [[guid|Display Text]]
    /// </summary>
    /// <param name="body">The markdown body to parse.</param>
    /// <returns>Collection of parsed links with target ID, display text, and position.</returns>
    IEnumerable<ParsedLink> ParseLinks(string? body);
}

/// <summary>
/// Represents a parsed wiki link from article markdown.
/// </summary>
/// <param name="TargetArticleId">The GUID of the article being linked to.</param>
/// <param name="DisplayText">Custom display text, or null to use target article's title.</param>
/// <param name="Position">Character offset in the source body where this link appears.</param>
public record ParsedLink(Guid TargetArticleId, string? DisplayText, int Position);
