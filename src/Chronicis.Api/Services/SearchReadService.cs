using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class SearchReadService : ISearchReadService
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleHierarchyService _hierarchyService;
    private readonly IReadAccessPolicyService _readAccessPolicy;

    public SearchReadService(
        ChronicisDbContext context,
        IArticleHierarchyService hierarchyService,
        IReadAccessPolicyService readAccessPolicy)
    {
        _context = context;
        _hierarchyService = hierarchyService;
        _readAccessPolicy = readAccessPolicy;
    }

    public async Task<GlobalSearchResultsDto> SearchAsync(string query, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new GlobalSearchResultsDto
            {
                Query = query ?? string.Empty,
                TitleMatches = new List<ArticleSearchResultDto>(),
                BodyMatches = new List<ArticleSearchResultDto>(),
                HashtagMatches = new List<ArticleSearchResultDto>(),
                TotalResults = 0
            };
        }

        var normalizedQuery = query.ToLowerInvariant();
        var readableWorldArticles = _readAccessPolicy
            .ApplyAuthenticatedWorldArticleFilter(_context.Articles.AsNoTracking(), userId);

        var titleMatches = await readableWorldArticles
            .Where(a => a.Title != null && a.Title.ToLower().Contains(normalizedQuery))
            .OrderBy(a => a.Title)
            .Take(20)
            .Select(a => new ArticleSearchResultDto
            {
                Id = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                MatchSnippet = a.Title ?? string.Empty,
                MatchType = "title",
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                AncestorPath = new List<BreadcrumbDto>()
            })
            .ToListAsync();

        var bodyMatches = await readableWorldArticles
            .Where(a => a.Body != null && a.Body.ToLower().Contains(normalizedQuery))
            .OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt)
            .Take(20)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Slug,
                a.Body,
                LastModified = a.ModifiedAt ?? a.CreatedAt
            })
            .ToListAsync();

        var bodyResults = bodyMatches.Select(a => new ArticleSearchResultDto
        {
            Id = a.Id,
            Title = a.Title ?? "Untitled",
            Slug = a.Slug,
            MatchSnippet = ExtractSnippet(a.Body ?? string.Empty, query, 100),
            MatchType = "content",
            LastModified = a.LastModified,
            AncestorPath = new List<BreadcrumbDto>()
        }).ToList();

        var hashtagQuery = $"#{query}";
        var hashtagMatches = await readableWorldArticles
            .Where(a => a.Body != null && a.Body.Contains(hashtagQuery))
            .OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt)
            .Take(20)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Slug,
                a.Body,
                LastModified = a.ModifiedAt ?? a.CreatedAt
            })
            .ToListAsync();

        var hashtagResults = hashtagMatches.Select(a => new ArticleSearchResultDto
        {
            Id = a.Id,
            Title = a.Title ?? "Untitled",
            Slug = a.Slug,
            MatchSnippet = ExtractSnippet(a.Body ?? string.Empty, hashtagQuery, 100),
            MatchType = "hashtag",
            LastModified = a.LastModified,
            AncestorPath = new List<BreadcrumbDto>()
        }).ToList();

        var allResults = titleMatches.Concat(bodyResults).Concat(hashtagResults).ToList();
        var ancestorOptions = new HierarchyWalkOptions
        {
            IncludeWorldBreadcrumb = false,
            IncludeCurrentArticle = false
        };

        foreach (var result in allResults)
        {
            result.AncestorPath = await _hierarchyService.BuildBreadcrumbsAsync(result.Id, ancestorOptions);
        }

        var seenIds = new HashSet<Guid>();
        var deduplicatedTitleMatches = titleMatches.Where(m => seenIds.Add(m.Id)).ToList();
        var deduplicatedBodyMatches = bodyResults.Where(m => seenIds.Add(m.Id)).ToList();
        var deduplicatedHashtagMatches = hashtagResults.Where(m => seenIds.Add(m.Id)).ToList();

        return new GlobalSearchResultsDto
        {
            Query = query,
            TitleMatches = deduplicatedTitleMatches,
            BodyMatches = deduplicatedBodyMatches,
            HashtagMatches = deduplicatedHashtagMatches,
            TotalResults = deduplicatedTitleMatches.Count + deduplicatedBodyMatches.Count + deduplicatedHashtagMatches.Count
        };
    }

    private static string ExtractSnippet(string text, string searchTerm, int contextLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return text.Length > contextLength * 2
                ? text[..(contextLength * 2)] + "..."
                : text;
        }

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + searchTerm.Length + contextLength);
        var snippet = text[start..end];

        if (start > 0)
        {
            snippet = "..." + snippet;
        }

        if (end < text.Length)
        {
            snippet += "...";
        }

        return CleanForDisplay(snippet);
    }

    private static string CleanForDisplay(string text)
    {
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"\[\[[a-fA-F0-9\-]{36}(?:\|([^\]]+))?\]\]", "$1");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}

