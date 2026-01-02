using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for automatically detecting and inserting wiki links in article content.
/// </summary>
public interface IAutoLinkService
{
    /// <summary>
    /// Scans article content and returns modified content with wiki links inserted
    /// for any text matching existing article titles.
    /// </summary>
    /// <param name="articleId">The article being edited (to exclude from matches).</param>
    /// <param name="worldId">The world to search for matching articles.</param>
    /// <param name="body">The article body content to scan.</param>
    /// <param name="userId">The user ID for scoping.</param>
    /// <returns>Response containing modified body and match details.</returns>
    Task<AutoLinkResponseDto> FindAndInsertLinksAsync(Guid articleId, Guid worldId, string body, Guid userId);
}

/// <summary>
/// Implementation of auto-link service.
/// </summary>
public class AutoLinkService : IAutoLinkService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<AutoLinkService> _logger;

    // Regex to find existing wiki links - we'll skip text inside these
    private static readonly Regex ExistingLinkPattern = new(
        @"\[\[([a-fA-F0-9\-]{36})(?:\|([^\]]+))?\]\]",
        RegexOptions.Compiled);

    public AutoLinkService(ChronicisDbContext context, ILogger<AutoLinkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AutoLinkResponseDto> FindAndInsertLinksAsync(
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
                ModifiedBody = body,
                Matches = new List<AutoLinkMatchDto>()
            };
        }

        // Get all articles in this world that could be linked to
        // User must have access to the world via WorldMembers
        // Exclude the current article and get titles with their IDs
        var linkableArticles = await (
            from a in _context.Articles
            join wm in _context.WorldMembers on a.WorldId equals wm.WorldId
            where wm.UserId == userId
            where a.WorldId == worldId
            where a.Id != articleId
            where !string.IsNullOrEmpty(a.Title)
            select new { a.Id, a.Title }
        ).ToListAsync();

        if (!linkableArticles.Any())
        {
            return new AutoLinkResponseDto
            {
                LinksFound = 0,
                ModifiedBody = body,
                Matches = new List<AutoLinkMatchDto>()
            };
        }

        // Sort by title length descending so we match longer titles first
        // This prevents "Water" from matching before "Waterdeep"
        var sortedArticles = linkableArticles
            .OrderByDescending(a => a.Title.Length)
            .ToList();

        var matches = new List<AutoLinkMatchDto>();
        var modifiedBody = body;

        foreach (var article in sortedArticles)
        {
            // Build regex for whole-word, case-insensitive match
            // Escape special regex characters in the title
            var escapedTitle = Regex.Escape(article.Title);
            var pattern = $@"\b{escapedTitle}\b";
            
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                
                // Replace ALL unlinked occurrences in the current modified body
                var (newBody, count, sampleMatch) = ReplaceAllUnlinkedOccurrences(modifiedBody, regex, article.Id);
                
                if (count > 0)
                {
                    // Record one match entry per article (with count)
                    matches.Add(new AutoLinkMatchDto
                    {
                        MatchedText = count > 1 ? $"{sampleMatch} ({count}x)" : sampleMatch,
                        ArticleTitle = article.Title,
                        ArticleId = article.Id
                    });
                    
                    modifiedBody = newBody;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create regex for title: {Title}", article.Title);
            }
        }

        // Total links = sum of all replacements
        var totalLinks = matches.Sum(m => 
        {
            // Extract count from "text (Nx)" format if present
            var match = Regex.Match(m.MatchedText, @"\((\d+)x\)$");
            return match.Success ? int.Parse(match.Groups[1].Value) : 1;
        });

        _logger.LogInformation(
            "Auto-link found {Count} matches ({TotalLinks} total links) for article {ArticleId}", 
            matches.Count, 
            totalLinks,
            articleId);

        return new AutoLinkResponseDto
        {
            LinksFound = totalLinks,
            ModifiedBody = modifiedBody,
            Matches = matches
        };
    }

    /// <summary>
    /// Gets ranges in the text that are already inside wiki links.
    /// </summary>
    private List<(int Start, int End)> GetProtectedRanges(string body)
    {
        var ranges = new List<(int Start, int End)>();
        
        foreach (Match match in ExistingLinkPattern.Matches(body))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        return ranges;
    }

    /// <summary>
    /// Checks if a position falls within a protected range.
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

    /// <summary>
    /// Replaces ALL occurrences of a pattern that are not already inside wiki links.
    /// Works backwards through the string to preserve match positions.
    /// </summary>
    private (string NewBody, int Count, string SampleMatch) ReplaceAllUnlinkedOccurrences(
        string body, 
        Regex pattern, 
        Guid articleId)
    {
        var protectedRanges = GetProtectedRanges(body);
        var allMatches = pattern.Matches(body);
        
        // Filter to only unlinked matches
        var unlinkedMatches = allMatches
            .Cast<Match>()
            .Where(m => !IsInProtectedRange(m.Index, m.Length, protectedRanges))
            .OrderByDescending(m => m.Index) // Process from end to start to preserve positions
            .ToList();

        if (unlinkedMatches.Count == 0)
        {
            return (body, 0, string.Empty);
        }

        var sampleMatch = unlinkedMatches.Last().Value; // First occurrence (last in our reversed list)
        var result = body;

        foreach (var match in unlinkedMatches)
        {
            // Replace this occurrence (working backwards preserves earlier positions)
            var replacement = $"[[{articleId}|{match.Value}]]";
            result = result.Substring(0, match.Index) 
                + replacement 
                + result.Substring(match.Index + match.Length);
        }

        return (result, unlinkedMatches.Count, sampleMatch);
    }
}
