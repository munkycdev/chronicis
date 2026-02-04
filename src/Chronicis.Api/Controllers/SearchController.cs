using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Global Search operations.
/// </summary>
[ApiController]
[Route("search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SearchController> _logger;

    // Regex to extract hashtags from content
    private static readonly Regex HashtagPattern = new(
        @"#([a-zA-Z][a-zA-Z0-9_]*)",
        RegexOptions.Compiled);

    public SearchController(
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<SearchController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/search?query={query}
    /// Searches across all article content the user has access to.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GlobalSearchResultsDto>> Search([FromQuery] string query)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new GlobalSearchResultsDto
            {
                Query = query ?? "",
                TitleMatches = new List<ArticleSearchResultDto>(),
                BodyMatches = new List<ArticleSearchResultDto>(),
                HashtagMatches = new List<ArticleSearchResultDto>(),
                TotalResults = 0
            });
        }

        _logger.LogDebug("Searching for '{Query}' for user {UserId}", query, user.Id);

        // Get all world IDs the user has access to
        var accessibleWorldIds = await _context.WorldMembers
            .Where(wm => wm.UserId == user.Id)
            .Select(wm => wm.WorldId)
            .ToListAsync();

        var normalizedQuery = query.ToLowerInvariant();

        // Title matches
        var titleMatches = await _context.Articles
            .Where(a => a.WorldId.HasValue && accessibleWorldIds.Contains(a.WorldId.Value))
            .Where(a => a.Title != null && a.Title.ToLower().Contains(normalizedQuery))
            .OrderBy(a => a.Title)
            .Take(20)
            .Select(a => new ArticleSearchResultDto
            {
                Id = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                MatchSnippet = a.Title ?? "",
                MatchType = "title",
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                AncestorPath = new List<BreadcrumbDto>() // Will be populated below
            })
            .ToListAsync();

        // Body content matches
        var bodyMatches = await _context.Articles
            .Where(a => a.WorldId.HasValue && accessibleWorldIds.Contains(a.WorldId.Value))
            .Where(a => a.Body != null && a.Body.ToLower().Contains(normalizedQuery))
            .OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt)
            .Take(20)
            .Select(a => new ArticleSearchResultDto
            {
                Id = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                MatchSnippet = "", // Will extract snippet below
                MatchType = "content",
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                AncestorPath = new List<BreadcrumbDto>()
            })
            .ToListAsync();

        // Extract snippets for body matches
        foreach (var match in bodyMatches)
        {
            var article = await _context.Articles.FindAsync(match.Id);
            if (article?.Body != null)
            {
                match.MatchSnippet = ExtractSnippet(article.Body, query, 100);
            }
        }

        // Hashtag matches (search for #query in body)
        var hashtagQuery = $"#{query}";
        var hashtagMatches = await _context.Articles
            .Where(a => a.WorldId.HasValue && accessibleWorldIds.Contains(a.WorldId.Value))
            .Where(a => a.Body != null && a.Body.Contains(hashtagQuery))
            .OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt)
            .Take(20)
            .Select(a => new ArticleSearchResultDto
            {
                Id = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                MatchSnippet = "", // Will extract snippet below
                MatchType = "hashtag",
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                AncestorPath = new List<BreadcrumbDto>()
            })
            .ToListAsync();

        // Extract snippets for hashtag matches
        foreach (var match in hashtagMatches)
        {
            var article = await _context.Articles.FindAsync(match.Id);
            if (article?.Body != null)
            {
                match.MatchSnippet = ExtractSnippet(article.Body, hashtagQuery, 100);
            }
        }

        // Build ancestor paths for all results
        var allResults = titleMatches.Concat(bodyMatches).Concat(hashtagMatches).ToList();
        foreach (var result in allResults)
        {
            result.AncestorPath = await BuildAncestorPathAsync(result.Id);
        }

        // Remove duplicates (same article appearing in multiple categories)
        var seenIds = new HashSet<Guid>();
        var deduplicatedTitleMatches = titleMatches.Where(m => seenIds.Add(m.Id)).ToList();
        var deduplicatedBodyMatches = bodyMatches.Where(m => seenIds.Add(m.Id)).ToList();
        var deduplicatedHashtagMatches = hashtagMatches.Where(m => seenIds.Add(m.Id)).ToList();

        var response = new GlobalSearchResultsDto
        {
            Query = query,
            TitleMatches = deduplicatedTitleMatches,
            BodyMatches = deduplicatedBodyMatches,
            HashtagMatches = deduplicatedHashtagMatches,
            TotalResults = deduplicatedTitleMatches.Count + deduplicatedBodyMatches.Count + deduplicatedHashtagMatches.Count
        };

        return Ok(response);
    }

    /// <summary>
    /// Extracts a snippet of text around the first occurrence of the search term.
    /// </summary>
    private static string ExtractSnippet(string text, string searchTerm, int contextLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return text.Length > contextLength * 2 ? text[..(contextLength * 2)] + "..." : text;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + searchTerm.Length + contextLength);

        var snippet = text[start..end];

        // Add ellipsis if we're not at the boundaries
        if (start > 0)
            snippet = "..." + snippet;
        if (end < text.Length)
            snippet += "...";

        // Clean up any HTML/markdown for display
        snippet = CleanForDisplay(snippet);

        return snippet;
    }

    /// <summary>
    /// Cleans text for display by removing HTML tags and normalizing whitespace.
    /// </summary>
    private static string CleanForDisplay(string text)
    {
        // Remove HTML tags
        text = Regex.Replace(text, @"<[^>]+>", " ");
        
        // Remove wiki link syntax [[guid|text]] or [[guid]]
        text = Regex.Replace(text, @"\[\[[a-fA-F0-9\-]{36}(?:\|([^\]]+))?\]\]", "$1");
        
        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");
        
        return text.Trim();
    }

    /// <summary>
    /// Builds the ancestor path (breadcrumbs) for an article.
    /// </summary>
    private async Task<List<BreadcrumbDto>> BuildAncestorPathAsync(Guid articleId)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var currentId = articleId;
        var visited = new HashSet<Guid>();

        // First, get the article itself to find its parent
        var article = await _context.Articles
            .Where(a => a.Id == currentId)
            .Select(a => new { a.ParentId })
            .FirstOrDefaultAsync();

        if (article?.ParentId == null)
            return breadcrumbs;

        currentId = article.ParentId.Value;

        // Walk up the tree from parent
        while (!visited.Contains(currentId))
        {
            visited.Add(currentId);

            var ancestor = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => new BreadcrumbDto
                {
                    Id = a.Id,
                    Title = a.Title ?? "Untitled",
                    Slug = a.Slug,
                    Type = a.Type,
                    IsWorld = false
                })
                .FirstOrDefaultAsync();

            if (ancestor == null)
                break;

            breadcrumbs.Insert(0, ancestor);

            var parent = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => a.ParentId)
                .FirstOrDefaultAsync();

            if (parent == null)
                break;

            currentId = parent.Value;
        }

        return breadcrumbs;
    }
}
