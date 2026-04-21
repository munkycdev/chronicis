using System.Net;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Regex-based implementation of <see cref="IWikiLinkTitleRewriter"/>.
/// Operates on HTML string content produced by the TipTap editor.
/// No external HTML parser dependency — uses a single <see cref="GeneratedRegex"/>
/// mirroring the span format used by the wiki-link TipTap extension.
/// </summary>
public sealed partial class WikiLinkTitleRewriter : IWikiLinkTitleRewriter
{
    /// <inheritdoc/>
    public (string Body, bool Changed) Rewrite(string? body, Guid targetArticleId, string newTitle)
    {
        if (string.IsNullOrEmpty(body))
            return (string.Empty, false);

        var targetIdStr  = targetArticleId.ToString("D");
        var encodedTitle = WebUtility.HtmlEncode(newTitle);
        var changed      = false;

        var result = WikiLinkSpanRegex().Replace(body, match =>
        {
            var attrs     = match.Groups[1].Value;
            var innerText = match.Groups[2].Value;

            // Must be a wiki-link type span
            if (!attrs.Contains("data-type=\"wiki-link\"", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            // Must target the renamed article (case-insensitive GUID match)
            if (!attrs.Contains($"data-target-id=\"{targetIdStr}\"", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            // Skip if user supplied a custom label — presence of data-display= disqualifies,
            // regardless of value (including empty string).
            if (attrs.Contains("data-display=", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            // Skip map chips (wiki-link spans with data-map-id attribute)
            if (attrs.Contains("data-map-id=", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            // Skip spans explicitly marked broken
            if (attrs.Contains("data-broken=\"true\"", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            changed = true;
            return $"<span{attrs}>{encodedTitle}</span>";
        });

        return (result, changed);
    }

    /// <summary>
    /// Matches any &lt;span&gt; element, capturing the attribute block (group 1) and
    /// plain-text inner content (group 2).  <c>[^&lt;]*</c> in group 2 ensures spans
    /// with nested markup are not matched, keeping inner-text-only spans eligible.
    /// </summary>
    [GeneratedRegex(@"<span(\s[^>]*?)>([^<]*)</span>", RegexOptions.IgnoreCase)]
    private static partial Regex WikiLinkSpanRegex();
}
