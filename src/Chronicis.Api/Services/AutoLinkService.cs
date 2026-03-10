using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for automatically detecting and inserting wiki links in article content.
/// </summary>
public interface IAutoLinkService
{
    /// <summary>
    /// Scans article content and returns match positions for wiki links.
    /// The client uses these positions to insert links via TipTap.
    /// </summary>
    /// <param name="articleId">The article being edited (to exclude from matches).</param>
    /// <param name="worldId">The world to search for matching articles.</param>
    /// <param name="body">The article body content (HTML) to scan.</param>
    /// <param name="userId">The user ID for scoping.</param>
    /// <returns>Response containing match positions and details.</returns>
    Task<AutoLinkResponseDto> FindLinksAsync(Guid articleId, Guid worldId, string body, Guid userId);
}

/// <summary>
/// Implementation of auto-link service.
/// Works on HTML content and returns match positions for client-side insertion.
/// </summary>
public sealed partial class AutoLinkService : IAutoLinkService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<AutoLinkService> _logger;

    public AutoLinkService(ChronicisDbContext context, ILogger<AutoLinkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AutoLinkResponseDto> FindLinksAsync(
        Guid articleId,
        Guid worldId,
        string body,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new AutoLinkResponseDto
            {
                LinksFound = 0,
                Matches = new List<AutoLinkMatchDto>()
            };
        }

        // Get all articles in this world that could be linked to (with their aliases)
        var linkableArticles = await (
            from a in _context.Articles
            join wm in _context.WorldMembers on a.WorldId equals wm.WorldId
            where wm.UserId == userId
            where a.WorldId == worldId
            where a.Id != articleId
            where !string.IsNullOrEmpty(a.Title)
            select new
            {
                a.Id,
                a.Title,
                Aliases = a.Aliases.Select(al => al.AliasText).ToList()
            }
        ).ToListAsync();

        if (!linkableArticles.Any())
        {
            return new AutoLinkResponseDto
            {
                LinksFound = 0,
                Matches = new List<AutoLinkMatchDto>()
            };
        }

        // Build protected ranges (areas we should not match in)
        var protectedRanges = GetProtectedRanges(body);

        // Build a list of all searchable terms (titles + aliases) with their article info
        // Each term knows whether it's an alias or the canonical title
        var searchTerms = new List<(string Term, Guid ArticleId, string ArticleTitle, bool IsAlias)>();

        foreach (var article in linkableArticles)
        {
            // Add the title
            searchTerms.Add((article.Title, article.Id, article.Title, false));

            // Add all aliases
            foreach (var alias in article.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    searchTerms.Add((alias, article.Id, article.Title, true));
                }
            }
        }

        // Sort by term length descending so we match longer terms first
        // This prevents "Water" from matching before "Waterdeep"
        var sortedTerms = searchTerms
            .OrderByDescending(t => t.Term.Length)
            .ToList();

        var allMatches = new List<AutoLinkMatchDto>();
        var usedRanges = new List<(int Start, int End)>(); // Track ranges we've already matched

        foreach (var term in sortedTerms)
        {
            // Build regex for whole-word, case-insensitive match
            var escapedTerm = Regex.Escape(term.Term);
            var pattern = $@"\b{escapedTerm}\b";

            try
            {
                var regexMatches = Regex.Matches(body, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in regexMatches)
                {
                    // Skip if this position is in a protected range (HTML tag, existing link, etc.)
                    if (IsInProtectedRange(match.Index, match.Length, protectedRanges))
                    {
                        continue;
                    }

                    // Skip if this position overlaps with an already-matched range
                    if (IsInProtectedRange(match.Index, match.Length, usedRanges))
                    {
                        continue;
                    }

                    // Valid match - record it
                    allMatches.Add(new AutoLinkMatchDto
                    {
                        MatchedText = match.Value,
                        ArticleTitle = term.ArticleTitle,
                        ArticleId = term.ArticleId,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length,
                        IsAliasMatch = term.IsAlias
                    });

                    // Mark this range as used so shorter terms don't match within it
                    usedRanges.Add((match.Index, match.Index + match.Length));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarningSanitized(ex, "Failed to create regex for term: {Term}", term.Term);
            }
        }

        // Sort matches by position for consistent display in confirmation dialog
        allMatches = allMatches.OrderBy(m => m.StartIndex).ToList();

        _logger.LogTraceSanitized(
            "Auto-link found {Count} matches for article {ArticleId}",
            allMatches.Count,
            articleId);

        return new AutoLinkResponseDto
        {
            LinksFound = allMatches.Count,
            Matches = allMatches
        };
    }

    /// <summary>
    /// Gets ranges in the content that should not be matched:
    /// - HTML tags
    /// - Existing wiki-link spans
    /// - Existing external-link spans
    /// - Legacy markdown wiki links
    /// </summary>
    private List<(int Start, int End)> GetProtectedRanges(string body)
    {
        var ranges = new List<(int Start, int End)>();

        // Protect HTML tags
        foreach (Match match in HtmlTagRegex().Matches(body))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        // Protect existing wiki-link spans (entire span including content)
        foreach (Match match in ExistingWikiLinkRegex().Matches(body))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        // Protect existing external-link spans
        foreach (Match match in ExistingExternalLinkRegex().Matches(body))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        // Protect legacy markdown wiki links (for mixed content)
        foreach (Match match in LegacyWikiLinkRegex().Matches(body))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        return ranges;
    }

    /// <summary>
    /// Checks if a position falls within any protected range.
    /// </summary>
    private bool IsInProtectedRange(int index, int length, List<(int Start, int End)> ranges)
    {
        foreach (var range in ranges)
        {
            // Check if any part of the match overlaps with the protected range
            if (index < range.End && index + length > range.Start)
            {
                return true;
            }
        }
        return false;
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<span[^>]*data-type=""wiki-link""[^>]*>.*?</span>", RegexOptions.Singleline)]
    private static partial Regex ExistingWikiLinkRegex();

    [GeneratedRegex(@"<span[^>]*data-type=""external-link""[^>]*>.*?</span>", RegexOptions.Singleline)]
    private static partial Regex ExistingExternalLinkRegex();

    [GeneratedRegex(@"\[\[([a-fA-F0-9\-]{36})(?:\|([^\]]+))?\]\]")]
    private static partial Regex LegacyWikiLinkRegex();
}
