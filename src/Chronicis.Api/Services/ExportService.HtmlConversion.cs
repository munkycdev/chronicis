using System.Text;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Services;

public partial class ExportService
{
    /// <summary>
    /// Convert HTML (from TipTap) to Markdown for export
    /// </summary>
    private string HtmlToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var markdown = html;

        // Wiki links: <span data-type="wiki-link" data-target-id="guid" data-display="display">text</span>
        // Convert to: [[display]] (we lose the GUID link, but preserve the text)
        markdown = Regex.Replace(markdown,
            @"<span[^>]*data-type=""wiki-link""[^>]*data-display=""([^""]+)""[^>]*>.*?</span>",
            "[[$1]]", RegexOptions.IgnoreCase);
        markdown = Regex.Replace(markdown,
            @"<span[^>]*data-type=""wiki-link""[^>]*>([^<]+)</span>",
            "[[$1]]", RegexOptions.IgnoreCase);

        // Headers
        markdown = Regex.Replace(markdown, @"<h1[^>]*>(.*?)</h1>", "# $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<h2[^>]*>(.*?)</h2>", "## $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<h3[^>]*>(.*?)</h3>", "### $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<h4[^>]*>(.*?)</h4>", "#### $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<h5[^>]*>(.*?)</h5>", "##### $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<h6[^>]*>(.*?)</h6>", "###### $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Bold and italic
        markdown = Regex.Replace(markdown, @"<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<b[^>]*>(.*?)</b>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        markdown = Regex.Replace(markdown, @"<i[^>]*>(.*?)</i>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Links
        markdown = Regex.Replace(markdown, @"<a[^>]*href=""([^""]*)""[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Code blocks
        markdown = Regex.Replace(markdown, @"<pre[^>]*><code[^>]*>([\s\S]*?)</code></pre>", "```\n$1\n```\n\n", RegexOptions.IgnoreCase);

        // Inline code
        markdown = Regex.Replace(markdown, @"<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Blockquotes
        markdown = Regex.Replace(markdown, @"<blockquote[^>]*>([\s\S]*?)</blockquote>", m =>
        {
            var content = m.Groups[1].Value;
            content = Regex.Replace(content, @"<p[^>]*>(.*?)</p>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var lines = content.Split('\n').Select(l => "> " + l.Trim()).Where(l => l != "> ");
            return string.Join("\n", lines) + "\n\n";
        }, RegexOptions.IgnoreCase);

        // Handle nested lists recursively
        markdown = ConvertListsToMarkdown(markdown);

        // Paragraphs
        markdown = Regex.Replace(markdown, @"<p[^>]*>(.*?)</p>", "$1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Line breaks
        markdown = Regex.Replace(markdown, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

        // Horizontal rules
        markdown = Regex.Replace(markdown, @"<hr[^>]*/?>", "\n---\n\n", RegexOptions.IgnoreCase);

        // Remove any remaining HTML tags
        markdown = Regex.Replace(markdown, @"<[^>]+>", "");

        // Decode HTML entities
        markdown = System.Net.WebUtility.HtmlDecode(markdown);

        // Clean up multiple newlines
        markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
        markdown = markdown.Trim();

        return markdown;
    }

    /// <summary>
    /// Convert HTML lists (including nested) to Markdown
    /// </summary>
    private string ConvertListsToMarkdown(string html)
    {
        // Process lists from innermost to outermost
        var result = html;
        var previousResult = "";

        // Keep processing until no more changes (handles deep nesting)
        while (result != previousResult)
        {
            previousResult = result;

            // Unordered lists
            result = Regex.Replace(result, @"<ul[^>]*>([\s\S]*?)</ul>", m =>
            {
                return ProcessList(m.Groups[1].Value, false, 0);
            }, RegexOptions.IgnoreCase);

            // Ordered lists
            result = Regex.Replace(result, @"<ol[^>]*>([\s\S]*?)</ol>", m =>
            {
                return ProcessList(m.Groups[1].Value, true, 0);
            }, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private string ProcessList(string listContent, bool ordered, int indentLevel)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indentLevel * 2);
        var counter = 1;

        // Match list items, being careful with nested content
        var liPattern = @"<li[^>]*>([\s\S]*?)</li>";
        var matches = Regex.Matches(listContent, liPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var itemContent = match.Groups[1].Value;

            // Check for nested lists
            var hasNestedUl = Regex.IsMatch(itemContent, @"<ul[^>]*>", RegexOptions.IgnoreCase);
            var hasNestedOl = Regex.IsMatch(itemContent, @"<ol[^>]*>", RegexOptions.IgnoreCase);

            // Extract text before any nested list
            string textContent;
            string nestedListContent = "";

            if (hasNestedUl || hasNestedOl)
            {
                var nestedListMatch = Regex.Match(itemContent, @"(<[uo]l[^>]*>[\s\S]*</[uo]l>)", RegexOptions.IgnoreCase);
                if (nestedListMatch.Success)
                {
                    var nestedListStart = nestedListMatch.Index;
                    textContent = itemContent.Substring(0, nestedListStart);
                    nestedListContent = nestedListMatch.Groups[1].Value;
                }
                else
                {
                    textContent = itemContent;
                }
            }
            else
            {
                textContent = itemContent;
            }

            // Clean up text content (remove p tags, etc.)
            textContent = Regex.Replace(textContent, @"<p[^>]*>(.*?)</p>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            textContent = Regex.Replace(textContent, @"<[^>]+>", "").Trim();

            // Write the list item
            var prefix = ordered ? $"{counter}. " : "- ";
            sb.AppendLine($"{indent}{prefix}{textContent}");
            counter++;

            // Process nested list with increased indent
            if (!string.IsNullOrEmpty(nestedListContent))
            {
                // Process nested unordered list
                var nestedUlMatch = Regex.Match(nestedListContent, @"<ul[^>]*>([\s\S]*?)</ul>", RegexOptions.IgnoreCase);
                if (nestedUlMatch.Success)
                {
                    var nestedResult = ProcessList(nestedUlMatch.Groups[1].Value, false, indentLevel + 1);
                    sb.Append(nestedResult);
                }

                // Process nested ordered list
                var nestedOlMatch = Regex.Match(nestedListContent, @"<ol[^>]*>([\s\S]*?)</ol>", RegexOptions.IgnoreCase);
                if (nestedOlMatch.Success)
                {
                    var nestedResult = ProcessList(nestedOlMatch.Groups[1].Value, true, indentLevel + 1);
                    sb.Append(nestedResult);
                }
            }
        }

        if (indentLevel == 0)
        {
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
