using System.Text;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Converts TipTap HTML content to Markdown for export.
/// Pure text transformation with no external dependencies.
/// </summary>
public static partial class HtmlToMarkdownConverter
{
    // ── Compiled regex patterns ──────────────────────────────────────────────

    [GeneratedRegex(@"<span[^>]*data-type=""wiki-link""[^>]*data-display=""([^""]+)""[^>]*>.*?</span>", RegexOptions.IgnoreCase)]
    private static partial Regex WikiLinkWithDisplay();

    [GeneratedRegex(@"<span[^>]*data-type=""wiki-link""[^>]*>([^<]+)</span>", RegexOptions.IgnoreCase)]
    private static partial Regex WikiLinkPlain();

    [GeneratedRegex(@"<strong[^>]*>(.*?)</strong>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StrongTag();

    [GeneratedRegex(@"<b[^>]*>(.*?)</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BoldTag();

    [GeneratedRegex(@"<em[^>]*>(.*?)</em>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex EmTag();

    [GeneratedRegex(@"<i[^>]*>(.*?)</i>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ItalicTag();

    [GeneratedRegex(@"<a[^>]*href=""([^""]*)""[^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorTag();

    [GeneratedRegex(@"<pre[^>]*><code[^>]*>([\s\S]*?)</code></pre>", RegexOptions.IgnoreCase)]
    private static partial Regex PreCodeBlock();

    [GeneratedRegex(@"<code[^>]*>(.*?)</code>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlineCode();

    [GeneratedRegex(@"<blockquote[^>]*>([\s\S]*?)</blockquote>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockquoteTag();

    [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ParagraphTag();

    [GeneratedRegex(@"<ul[^>]*>([\s\S]*)</ul>", RegexOptions.IgnoreCase)]
    private static partial Regex UnorderedList();

    [GeneratedRegex(@"<ol[^>]*>([\s\S]*)</ol>", RegexOptions.IgnoreCase)]
    private static partial Regex OrderedList();

    [GeneratedRegex(@"<li[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemOpen();

    [GeneratedRegex(@"</li>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemClose();

    [GeneratedRegex(@"(<[uo]l[^>]*>[\s\S]*</[uo]l>)", RegexOptions.IgnoreCase)]
    private static partial Regex NestedList();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTag();

    [GeneratedRegex(@"<hr[^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HorizontalRule();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlines();

    /// <summary>
    /// Compiled header patterns for h1–h6. Index 0 = h1, index 5 = h6.
    /// </summary>
    private static readonly Regex[] HeaderPatterns =
    [
        new Regex(@"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
        new Regex(@"<h2[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
        new Regex(@"<h3[^>]*>(.*?)</h3>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
        new Regex(@"<h4[^>]*>(.*?)</h4>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
        new Regex(@"<h5[^>]*>(.*?)</h5>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
        new Regex(@"<h6[^>]*>(.*?)</h6>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled),
    ];

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Converts HTML content to Markdown.
    /// </summary>
    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var markdown = html;

        markdown = ConvertWikiLinks(markdown);
        markdown = ConvertHeaders(markdown);
        markdown = ConvertInlineFormatting(markdown);
        markdown = ConvertLinks(markdown);
        markdown = ConvertCodeBlocks(markdown);
        markdown = ConvertBlockquotes(markdown);
        markdown = ConvertLists(markdown);
        markdown = ConvertParagraphsAndBreaks(markdown);
        markdown = StripRemainingTags(markdown);
        markdown = System.Net.WebUtility.HtmlDecode(markdown);
        markdown = NormalizeWhitespace(markdown);

        return markdown;
    }

    // ── Wiki Links ──────────────────────────────────────────

    internal static string ConvertWikiLinks(string html)
    {
        var result = WikiLinkWithDisplay().Replace(html, "[[$1]]");
        return WikiLinkPlain().Replace(result, "[[$1]]");
    }

    // ── Headers ─────────────────────────────────────────────

    internal static string ConvertHeaders(string html)
    {
        var result = html;
        for (int i = 0; i < 6; i++)
        {
            var prefix = new string('#', i + 1);
            result = HeaderPatterns[i].Replace(result, $"{prefix} $1\n\n");
        }
        return result;
    }

    // ── Inline Formatting ───────────────────────────────────

    internal static string ConvertInlineFormatting(string html)
    {
        var result = StrongTag().Replace(html, "**$1**");
        result = BoldTag().Replace(result, "**$1**");
        result = EmTag().Replace(result, "*$1*");
        return ItalicTag().Replace(result, "*$1*");
    }

    // ── Links ───────────────────────────────────────────────

    internal static string ConvertLinks(string html)
        => AnchorTag().Replace(html, "[$2]($1)");

    // ── Code ────────────────────────────────────────────────

    internal static string ConvertCodeBlocks(string html)
    {
        var result = PreCodeBlock().Replace(html, "```\n$1\n```\n\n");
        return InlineCode().Replace(result, "`$1`");
    }

    // ── Blockquotes ─────────────────────────────────────────

    internal static string ConvertBlockquotes(string html)
    {
        return BlockquoteTag().Replace(html, m =>
        {
            var content = m.Groups[1].Value;
            content = ParagraphTag().Replace(content, "$1");
            var lines = content.Split('\n')
                .Select(l => "> " + l.Trim())
                .Where(l => l != "> ");
            return string.Join("\n", lines) + "\n\n";
        });
    }

    // ── Lists ───────────────────────────────────────────────

    internal static string ConvertLists(string html)
    {
        var result = html;
        var previous = "";

        // Keep processing until no more changes (handles deep nesting)
        while (result != previous)
        {
            previous = result;

            // Use greedy match so outermost list is captured first;
            // ProcessList handles nested <ul>/<ol> recursively within each <li>.
            result = UnorderedList().Replace(result,
                m => ProcessList(m.Groups[1].Value, ordered: false, indentLevel: 0));
            result = OrderedList().Replace(result,
                m => ProcessList(m.Groups[1].Value, ordered: true, indentLevel: 0));
        }

        return result;
    }

    internal static string ProcessList(string listContent, bool ordered, int indentLevel)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indentLevel * 2);
        var counter = 1;

        // Extract list items accounting for nested lists.
        // We track <li> depth so we match the correct closing </li>.
        var items = ExtractListItems(listContent);

        foreach (var itemContent in items)
        {
            var (textContent, nestedListHtml) = SplitNestedList(itemContent);

            textContent = StripInlineTags(textContent);

            var prefix = ordered ? $"{counter}. " : "- ";
            sb.AppendLine($"{indent}{prefix}{textContent}");
            counter++;

            if (!string.IsNullOrEmpty(nestedListHtml))
            {
                sb.Append(RenderNestedList(nestedListHtml, indentLevel + 1));
            }
        }

        if (indentLevel == 0)
            sb.AppendLine();

        return sb.ToString();
    }

    private static List<string> ExtractListItems(string listContent)
    {
        var items = new List<string>();
        var liOpens = ListItemOpen().Matches(listContent);

        foreach (Match openMatch in liOpens)
        {
            var start = openMatch.Index + openMatch.Length;
            var depth = 1;
            var pos = start;

            while (pos < listContent.Length && depth > 0)
            {
                var nextOpen = ListItemOpen().Match(listContent[pos..]);
                var nextClose = ListItemClose().Match(listContent[pos..]);

                if (!nextClose.Success)
                    break;

                if (nextOpen.Success && nextOpen.Index < nextClose.Index)
                {
                    depth++;
                    pos += nextOpen.Index + nextOpen.Length;
                }
                else
                {
                    depth--;
                    if (depth == 0)
                    {
                        items.Add(listContent[start..(pos + nextClose.Index)]);
                    }
                    pos += nextClose.Index + nextClose.Length;
                }
            }
        }

        return items;
    }

    private static (string text, string nestedHtml) SplitNestedList(string itemContent)
    {
        var nestedMatch = NestedList().Match(itemContent);
        if (!nestedMatch.Success)
            return (itemContent, "");

        var text = itemContent[..nestedMatch.Index];
        return (text, nestedMatch.Groups[1].Value);
    }

    private static string RenderNestedList(string nestedHtml, int indentLevel)
    {
        var sb = new StringBuilder();

        var ulMatch = UnorderedList().Match(nestedHtml);
        if (ulMatch.Success)
            sb.Append(ProcessList(ulMatch.Groups[1].Value, ordered: false, indentLevel));

        var olMatch = OrderedList().Match(nestedHtml);
        if (olMatch.Success)
            sb.Append(ProcessList(olMatch.Groups[1].Value, ordered: true, indentLevel));

        return sb.ToString();
    }

    private static string StripInlineTags(string text)
    {
        var result = ParagraphTag().Replace(text, "$1");
        return AnyTag().Replace(result, "").Trim();
    }

    // ── Paragraphs & Breaks ─────────────────────────────────

    internal static string ConvertParagraphsAndBreaks(string html)
    {
        var result = ParagraphTag().Replace(html, "$1\n\n");
        result = BreakTag().Replace(result, "\n");
        return HorizontalRule().Replace(result, "\n---\n\n");
    }

    // ── Cleanup ─────────────────────────────────────────────

    internal static string StripRemainingTags(string html)
        => AnyTag().Replace(html, "");

    internal static string NormalizeWhitespace(string text)
        => ExcessiveNewlines().Replace(text, "\n\n").Trim();
}
