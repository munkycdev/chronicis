using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for exporting world data to downloadable archives
/// </summary>
public class ExportService : IExportService
{
    private readonly ChronicisDbContext _db;
    private readonly IWorldMembershipService _membershipService;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        ChronicisDbContext db,
        IWorldMembershipService membershipService,
        ILogger<ExportService> logger)
    {
        _db = db;
        _membershipService = membershipService;
        _logger = logger;
    }

    public async Task<byte[]?> ExportWorldToMarkdownAsync(Guid worldId, Guid userId)
    {
        // Verify user has access to the world
        if (!await _membershipService.UserHasAccessAsync(worldId, userId))
        {
            _logger.LogWarning("User {UserId} attempted to export world {WorldId} without access", userId, worldId);
            return null;
        }

        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
        {
            _logger.LogWarning("World {WorldId} not found for export", worldId);
            return null;
        }

        _logger.LogDebug("Starting export of world {WorldId} ({WorldName}) for user {UserId}", 
            worldId, world.Name, userId);

        // Get all articles for this world with their hierarchy info
        var articles = await _db.Articles
            .AsNoTracking()
            .Where(a => a.WorldId == worldId)
            .OrderBy(a => a.Title)
            .ToListAsync();

        // Get campaigns and arcs for building folder structure
        var campaigns = await _db.Campaigns
            .AsNoTracking()
            .Where(c => c.WorldId == worldId)
            .ToListAsync();

        var arcs = await _db.Arcs
            .AsNoTracking()
            .Where(a => campaigns.Select(c => c.Id).Contains(a.CampaignId))
            .ToListAsync();

        // Build the zip archive
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var worldFolderName = SanitizeFileName(world.Name);

            // Export wiki articles (hierarchical)
            var wikiArticles = articles.Where(a => a.Type == ArticleType.WikiArticle || a.Type == ArticleType.Legacy).ToList();
            await ExportArticleHierarchy(archive, wikiArticles, $"{worldFolderName}/Wiki", null);

            // Export characters
            var characters = articles.Where(a => a.Type == ArticleType.Character).ToList();
            await ExportArticleHierarchy(archive, characters, $"{worldFolderName}/Characters", null);

            // Export campaigns with their sessions
            foreach (var campaign in campaigns)
            {
                var campaignFolder = $"{worldFolderName}/Campaigns/{SanitizeFileName(campaign.Name)}";
                
                // Campaign info file
                var campaignContent = BuildCampaignMarkdown(campaign);
                await AddFileToArchive(archive, $"{campaignFolder}/{SanitizeFileName(campaign.Name)}.md", campaignContent);

                // Export arcs and sessions
                var campaignArcs = arcs.Where(a => a.CampaignId == campaign.Id).OrderBy(a => a.SortOrder).ToList();
                foreach (var arc in campaignArcs)
                {
                    var arcFolder = $"{campaignFolder}/{SanitizeFileName(arc.Name)}";
                    
                    // Arc info file
                    var arcContent = BuildArcMarkdown(arc);
                    await AddFileToArchive(archive, $"{arcFolder}/{SanitizeFileName(arc.Name)}.md", arcContent);

                    // Sessions in this arc
                    var sessions = articles
                        .Where(a => a.ArcId == arc.Id && a.Type == ArticleType.Session)
                        .OrderBy(a => a.SessionDate ?? a.EffectiveDate)
                        .ToList();

                    foreach (var session in sessions)
                    {
                        var sessionContent = BuildArticleMarkdown(session);
                        var sessionFileName = SanitizeFileName(session.Title);
                        await AddFileToArchive(archive, $"{arcFolder}/{sessionFileName}/{sessionFileName}.md", sessionContent);

                        // Session notes as children
                        var sessionNotes = articles.Where(a => a.ParentId == session.Id).ToList();
                        foreach (var note in sessionNotes)
                        {
                            var noteContent = BuildArticleMarkdown(note);
                            var noteFileName = SanitizeFileName(note.Title);
                            await AddFileToArchive(archive, $"{arcFolder}/{sessionFileName}/{noteFileName}.md", noteContent);
                        }
                    }
                }
            }
        }

        memoryStream.Position = 0;
        var result = memoryStream.ToArray();
        
        _logger.LogDebug("Export completed for world {WorldId}. Archive size: {Size} bytes", 
            worldId, result.Length);

        return result;
    }

    private async Task ExportArticleHierarchy(
        ZipArchive archive, 
        List<Article> articles, 
        string basePath, 
        Guid? parentId)
    {
        var children = articles.Where(a => a.ParentId == parentId).OrderBy(a => a.Title).ToList();

        foreach (var article in children)
        {
            var articleFileName = SanitizeFileName(article.Title);
            var hasChildren = articles.Any(a => a.ParentId == article.Id);

            if (hasChildren)
            {
                // Article with children: ArticleName/ArticleName.md
                var articleFolder = $"{basePath}/{articleFileName}";
                var content = BuildArticleMarkdown(article);
                await AddFileToArchive(archive, $"{articleFolder}/{articleFileName}.md", content);

                // Recursively export children
                await ExportArticleHierarchy(archive, articles, articleFolder, article.Id);
            }
            else
            {
                // Leaf article: ArticleName.md
                var content = BuildArticleMarkdown(article);
                await AddFileToArchive(archive, $"{basePath}/{articleFileName}.md", content);
            }
        }
    }

    private string BuildArticleMarkdown(Article article)
    {
        var sb = new StringBuilder();

        // YAML frontmatter
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(article.Title)}\"");
        sb.AppendLine($"type: {article.Type}");
        sb.AppendLine($"visibility: {article.Visibility}");
        sb.AppendLine($"created: {article.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (article.ModifiedAt.HasValue)
            sb.AppendLine($"modified: {article.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
        if (article.SessionDate.HasValue)
            sb.AppendLine($"session_date: {article.SessionDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(article.InGameDate))
            sb.AppendLine($"in_game_date: \"{EscapeYaml(article.InGameDate)}\"");
        if (!string.IsNullOrEmpty(article.IconEmoji))
            sb.AppendLine($"icon: \"{article.IconEmoji}\"");
        sb.AppendLine("---");
        sb.AppendLine();

        // Title
        sb.AppendLine($"# {article.Title}");
        sb.AppendLine();

        // Body content (convert HTML to Markdown)
        if (!string.IsNullOrEmpty(article.Body))
        {
            var markdown = HtmlToMarkdown(article.Body);
            sb.AppendLine(markdown);
        }

        // AI Summary section
        if (!string.IsNullOrEmpty(article.AISummary))
        {
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## AI Summary");
            sb.AppendLine();
            sb.AppendLine(article.AISummary);
            if (article.AISummaryGeneratedAt.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine($"*Generated: {article.AISummaryGeneratedAt:yyyy-MM-dd HH:mm:ss}*");
            }
        }

        return sb.ToString();
    }

    private string BuildCampaignMarkdown(Campaign campaign)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(campaign.Name)}\"");
        sb.AppendLine("type: Campaign");
        sb.AppendLine($"created: {campaign.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {campaign.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(campaign.Description))
        {
            sb.AppendLine(campaign.Description);
        }

        return sb.ToString();
    }

    private string BuildArcMarkdown(Arc arc)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(arc.Name)}\"");
        sb.AppendLine("type: Arc");
        sb.AppendLine($"sort_order: {arc.SortOrder}");
        sb.AppendLine($"created: {arc.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {arc.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(arc.Description))
        {
            sb.AppendLine(arc.Description);
        }

        return sb.ToString();
    }

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

    private static async Task AddFileToArchive(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Untitled";

        // Replace invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name);
        foreach (var c in invalid)
        {
            sanitized.Replace(c, '_');
        }

        // Also replace some characters that might cause issues
        sanitized.Replace('/', '_');
        sanitized.Replace('\\', '_');
        sanitized.Replace(':', '_');

        var result = sanitized.ToString().Trim();
        
        // Limit length
        if (result.Length > 100)
            result = result.Substring(0, 100);

        return string.IsNullOrWhiteSpace(result) ? "Untitled" : result;
    }

    private static string EscapeYaml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }
}
