using System.Text;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

/// <summary>
/// Converts TipTap HTML content to Markdown for export.
/// Pure text transformation with no external dependencies.
/// </summary>
public static class HtmlToMarkdownConverter
{
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
        var result = Regex.Replace(html,
            @"<span[^>]*data-type=""wiki-link""[^>]*data-display=""([^""]+)""[^>]*>.*?</span>",
            "[[$1]]", RegexOptions.IgnoreCase);
        return Regex.Replace(result,
            @"<span[^>]*data-type=""wiki-link""[^>]*>([^<]+)</span>",
            "[[$1]]", RegexOptions.IgnoreCase);
    }

    // ── Headers ─────────────────────────────────────────────

    internal static string ConvertHeaders(string html)
    {
        var result = html;
        for (int i = 1; i <= 6; i++)
        {
            var prefix = new string('#', i);
            result = Regex.Replace(result,
                $@"<h{i}[^>]*>(.*?)</h{i}>",
                $"{prefix} $1\n\n",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        return result;
    }

    // ── Inline Formatting ───────────────────────────────────

    internal static string ConvertInlineFormatting(string html)
    {
        var result = html;
        result = Regex.Replace(result, @"<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<b[^>]*>(.*?)</b>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<i[^>]*>(.*?)</i>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return result;
    }

    // ── Links ───────────────────────────────────────────────

    internal static string ConvertLinks(string html)
    {
        return Regex.Replace(html,
            @"<a[^>]*href=""([^""]*)""[^>]*>(.*?)</a>",
            "[$2]($1)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    // ── Code ────────────────────────────────────────────────

    internal static string ConvertCodeBlocks(string html)
    {
        var result = Regex.Replace(html,
            @"<pre[^>]*><code[^>]*>([\s\S]*?)</code></pre>",
            "```\n$1\n```\n\n", RegexOptions.IgnoreCase);
        return Regex.Replace(result,
            @"<code[^>]*>(.*?)</code>",
            "`$1`", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    // ── Blockquotes ─────────────────────────────────────────

    internal static string ConvertBlockquotes(string html)
    {
        return Regex.Replace(html,
            @"<blockquote[^>]*>([\s\S]*?)</blockquote>", m =>
            {
                var content = m.Groups[1].Value;
                content = Regex.Replace(content, @"<p[^>]*>(.*?)</p>", "$1",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var lines = content.Split('\n')
                    .Select(l => "> " + l.Trim())
                    .Where(l => l != "> ");
                return string.Join("\n", lines) + "\n\n";
            }, RegexOptions.IgnoreCase);
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
            result = Regex.Replace(result, @"<ul[^>]*>([\s\S]*)</ul>",
                m => ProcessList(m.Groups[1].Value, ordered: false, indentLevel: 0),
                RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"<ol[^>]*>([\s\S]*)</ol>",
                m => ProcessList(m.Groups[1].Value, ordered: true, indentLevel: 0),
                RegexOptions.IgnoreCase);
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
        var liOpens = Regex.Matches(listContent, @"<li[^>]*>", RegexOptions.IgnoreCase);

        foreach (Match openMatch in liOpens)
        {
            var start = openMatch.Index + openMatch.Length;
            var depth = 1;
            var pos = start;

            while (pos < listContent.Length && depth > 0)
            {
                var nextOpen = Regex.Match(listContent[pos..], @"<li[^>]*>", RegexOptions.IgnoreCase);
                var nextClose = Regex.Match(listContent[pos..], @"</li>", RegexOptions.IgnoreCase);

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
        var nestedMatch = Regex.Match(itemContent, @"(<[uo]l[^>]*>[\s\S]*</[uo]l>)", RegexOptions.IgnoreCase);
        if (!nestedMatch.Success)
            return (itemContent, "");

        var text = itemContent[..nestedMatch.Index];
        return (text, nestedMatch.Groups[1].Value);
    }

    private static string RenderNestedList(string nestedHtml, int indentLevel)
    {
        var sb = new StringBuilder();

        var ulMatch = Regex.Match(nestedHtml, @"<ul[^>]*>([\s\S]*)</ul>", RegexOptions.IgnoreCase);
        if (ulMatch.Success)
            sb.Append(ProcessList(ulMatch.Groups[1].Value, ordered: false, indentLevel));

        var olMatch = Regex.Match(nestedHtml, @"<ol[^>]*>([\s\S]*)</ol>", RegexOptions.IgnoreCase);
        if (olMatch.Success)
            sb.Append(ProcessList(olMatch.Groups[1].Value, ordered: true, indentLevel));

        return sb.ToString();
    }

    private static string StripInlineTags(string text)
    {
        var result = Regex.Replace(text, @"<p[^>]*>(.*?)</p>", "$1",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return Regex.Replace(result, @"<[^>]+>", "").Trim();
    }

    // ── Paragraphs & Breaks ─────────────────────────────────

    internal static string ConvertParagraphsAndBreaks(string html)
    {
        var result = Regex.Replace(html, @"<p[^>]*>(.*?)</p>", "$1\n\n",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<hr[^>]*/?>", "\n---\n\n", RegexOptions.IgnoreCase);
        return result;
    }

    // ── Cleanup ─────────────────────────────────────────────

    internal static string StripRemainingTags(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", "");
    }

    internal static string NormalizeWhitespace(string text)
    {
        var result = Regex.Replace(text, @"\n{3,}", "\n\n");
        return result.Trim();
    }
}
