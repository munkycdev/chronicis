using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for automatically inserting hashtags into article bodies
/// based on references to other article titles
/// </summary>
public class AutoHashtagService : IAutoHashtagService
{
    private readonly ChronicisDbContext _context;
    private readonly IHashtagSyncService _hashtagSync;
    private readonly ILogger<AutoHashtagService> _logger;

    public AutoHashtagService(
        ChronicisDbContext context,
        IHashtagSyncService hashtagSync,
        ILogger<AutoHashtagService> logger)
    {
        _context = context;
        _hashtagSync = hashtagSync;
        _logger = logger;
    }

    /// <summary>
    /// Process articles to find and optionally insert hashtags
    /// </summary>
    public async Task<AutoHashtagResponse> ProcessArticlesAsync(
        Guid userId,
        bool dryRun,
        List<Guid>? articleIds = null)
    {
        var response = new AutoHashtagResponse
        {
            WasDryRun = dryRun
        };

        // Get all articles for the user
        var articlesQuery = _context.Articles
            .AsNoTracking()
            .Where(a => a.CreatedBy == userId);

        if (articleIds != null && articleIds.Any())
        {
            articlesQuery = articlesQuery.Where(a => articleIds.Contains(a.Id));
        }

        var articles = await articlesQuery
            .Select(a => new { a.Id, a.Title, a.Body })
            .ToListAsync();

        response.TotalArticlesScanned = articles.Count;

        if (!articles.Any())
        {
            return response;
        }

        // Build title -> slug mapping (excluding very short titles to avoid false positives)
        var titleMappings = articles
            .Where(a => !string.IsNullOrWhiteSpace(a.Title) && a.Title.Length >= 3)
            .Select(a => new
            {
                a.Title,
                Slug = SlugGenerator.GenerateSlug(a.Title)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Slug))
            .GroupBy(x => x.Slug.ToLowerInvariant())
            .Select(g => g.First())
            .ToDictionary(x => x.Title, x => x.Slug, StringComparer.OrdinalIgnoreCase);

        // Process each article
        foreach (var article in articles)
        {
            if (string.IsNullOrWhiteSpace(article.Body))
                continue;

            var changes = FindHashtagOpportunities(article.Body, titleMappings);

            if (changes.MatchesFound > 0)
            {
                changes.ArticleId = article.Id;
                changes.ArticleTitle = article.Title ?? "(Untitled)";
                changes.OriginalBody = article.Body;

                response.Changes.Add(changes);
                response.TotalMatchesFound += changes.MatchesFound;

                // If not dry run, apply the changes
                if (!dryRun)
                {
                    await ApplyChangesAsync(article.Id, changes.PreviewBody);
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Find hashtag opportunities in article body
    /// </summary>
    private AutoHashtagChange FindHashtagOpportunities(
        string body,
        Dictionary<string, string> titleToSlugMap)
    {
        var change = new AutoHashtagChange();
        var modifiedBody = body;
        var matchedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var replacements = new List<(int Index, int Length, string Replacement, string Title)>();

        // Sort titles by length (longest first) to match longer phrases before shorter ones
        var sortedTitles = titleToSlugMap.Keys.OrderByDescending(t => t.Length).ToList();

        // First pass: Find all potential replacements
        foreach (var title in sortedTitles)
        {
            var slug = titleToSlugMap[title];

            // Create regex pattern for whole-word matching
            // \b = word boundary, ensures we match complete words
            var pattern = $@"\b{Regex.Escape(title)}\b";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var matches = regex.Matches(modifiedBody);

            foreach (Match match in matches)
            {
                // Skip if already a hashtag (preceded by #)
                if (match.Index > 0 && modifiedBody[match.Index - 1] == '#')
                    continue;

                // Skip if already processed as part of a longer match
                if (replacements.Any(r => match.Index >= r.Index && match.Index < r.Index + r.Length))
                    continue;

                // Check if this position is inside a markdown structure or existing hashtag
                if (IsInsideMarkdownStructure(modifiedBody, match.Index))
                    continue;

                // Check if this word is already part of an existing hashtag
                if (IsPartOfExistingHashtag(modifiedBody, match.Index, match.Length))
                    continue;

                // Valid match - add to replacements
                var replacement = $"<mark>#{slug}</mark>";
                replacements.Add((match.Index, match.Length, replacement, title));
            }
        }

        // Sort replacements by index (descending) to replace from end to start
        // This prevents index shifting issues
        replacements = replacements.OrderByDescending(r => r.Index).ToList();

        // Second pass: Apply replacements
        foreach (var (index, length, replacement, title) in replacements)
        {
            var before = modifiedBody.Substring(0, index);
            var after = modifiedBody.Substring(index + length);
            modifiedBody = before + replacement + after;

            matchedTitles.Add(title);
            change.MatchesFound++;
        }

        change.PreviewBody = modifiedBody;
        change.MatchedTitles = matchedTitles.ToList();

        return change;
    }

    /// <summary>
    /// Check if a position in text is inside markdown structure
    /// (links, code blocks, etc) where we shouldn't insert hashtags
    /// </summary>
    private bool IsInsideMarkdownStructure(string text, int position)
    {
        // Check for inline code (backticks)
        var beforeText = text.Substring(0, position);

        var backticksBefore = beforeText.Count(c => c == '`');

        // If odd number of backticks before this position, we're inside code
        if (backticksBefore % 2 != 0)
            return true;

        // Check for markdown links [text](url)
        var linkPattern = @"\[([^\]]+)\]\(([^\)]+)\)";
        var linkMatches = Regex.Matches(text, linkPattern);

        foreach (Match linkMatch in linkMatches)
        {
            if (position >= linkMatch.Index && position < linkMatch.Index + linkMatch.Length)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if text at position is already part of an existing hashtag
    /// </summary>
    private bool IsPartOfExistingHashtag(string text, int position, int length)
    {
        // Look backwards from position to see if there's a # before this word
        // We need to check if there's a # with no space between it and our word

        var checkStart = Math.Max(0, position - 50); // Look back up to 50 chars
        var checkLength = position - checkStart;

        if (checkLength > 0)
        {
            var beforeText = text.Substring(checkStart, checkLength);

            // Find all hashtags in the text before our position
            var hashtagPattern = @"#[a-z0-9_-]+";
            var hashtagMatches = Regex.Matches(beforeText, hashtagPattern, RegexOptions.IgnoreCase);

            foreach (Match hashtagMatch in hashtagMatches)
            {
                var hashtagEnd = checkStart + hashtagMatch.Index + hashtagMatch.Length;

                // If our match position falls within or immediately after this hashtag, skip it
                if (position >= checkStart + hashtagMatch.Index && position <= hashtagEnd)
                {
                    return true;
                }
            }
        }

        // Also check if the text ahead contains a hashtag that includes our word
        var checkEnd = Math.Min(text.Length, position + length + 50);
        var aheadText = text.Substring(position, checkEnd - position);

        // Check if our word is at the start of an existing hashtag
        if (aheadText.Length > length && aheadText[0] != '#')
        {
            // Look for # immediately before our position
            if (position > 0 && text[position - 1] == '#')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Apply hashtag changes to an article and sync hashtags to database
    /// </summary>
    private async Task ApplyChangesAsync(Guid articleId, string newBody)
    {
        // Remove <mark> tags from preview before saving
        var cleanedBody = Regex.Replace(newBody, @"<mark>|</mark>", string.Empty);

        var article = await _context.Articles.FindAsync(articleId);
        if (article != null)
        {
            article.Body = cleanedBody;
            article.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // IMPORTANT: Sync hashtags to the ArticleHashtag table
            // This will parse the hashtags in the body and create the proper database relationships
            await _hashtagSync.SyncHashtagsAsync(articleId, cleanedBody);

            _logger.LogInformation(
                "Applied auto-hashtag changes to article {ArticleId} and synced hashtags to database",
                articleId);
        }
    }
}
