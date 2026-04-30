using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public sealed partial class SearchReadService : ISearchReadService
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
                AncestorPath = new List<BreadcrumbDto>(),
                Type = a.Type,
                WorldSlug = a.World != null ? a.World.Slug : string.Empty,
                CampaignSlug = a.Session != null ? a.Session.Arc.Campaign.Slug : null,
                ArcSlug = a.Session != null ? a.Session.Arc.Slug : null,
                SessionSlug = a.Session != null ? a.Session.Slug : null
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
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                a.Type,
                WorldSlug = a.World != null ? a.World.Slug : string.Empty,
                CampaignSlug = a.Session != null ? a.Session.Arc.Campaign.Slug : null,
                ArcSlug = a.Session != null ? a.Session.Arc.Slug : null,
                SessionSlug = a.Session != null ? a.Session.Slug : null
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
            AncestorPath = new List<BreadcrumbDto>(),
            Type = a.Type,
            WorldSlug = a.WorldSlug,
            CampaignSlug = a.CampaignSlug,
            ArcSlug = a.ArcSlug,
            SessionSlug = a.SessionSlug
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
                LastModified = a.ModifiedAt ?? a.CreatedAt,
                a.Type,
                WorldSlug = a.World != null ? a.World.Slug : string.Empty,
                CampaignSlug = a.Session != null ? a.Session.Arc.Campaign.Slug : null,
                ArcSlug = a.Session != null ? a.Session.Arc.Slug : null,
                SessionSlug = a.Session != null ? a.Session.Slug : null
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
            AncestorPath = new List<BreadcrumbDto>(),
            Type = a.Type,
            WorldSlug = a.WorldSlug,
            CampaignSlug = a.CampaignSlug,
            ArcSlug = a.ArcSlug,
            SessionSlug = a.SessionSlug
        }).ToList();

        var allResults = titleMatches.Concat(bodyResults).Concat(hashtagResults).ToList();
        var ancestorOptions = new HierarchyWalkOptions
        {
            IncludeWorldBreadcrumb = false,
            IncludeCurrentArticle = false
        };

        var ancestorPaths = await _hierarchyService.BuildBreadcrumbsBatchAsync(
            allResults.Select(r => r.Id),
            ancestorOptions);

        foreach (var result in allResults)
        {
            if (ancestorPaths.TryGetValue(result.Id, out var path))
                result.AncestorPath = path;
            result.ArticleSlugChain = ComputeSlugChain(result);
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

    private static List<string> ComputeSlugChain(ArticleSearchResultDto result)
    {
        if (result.Type == ArticleType.SessionNote)
        {
            if (result.CampaignSlug != null && result.ArcSlug != null && result.SessionSlug != null)
                return [result.WorldSlug, result.CampaignSlug, result.ArcSlug, result.SessionSlug, result.Slug];
            return [result.Slug];
        }

        var chain = result.AncestorPath
            .Where(b => !b.IsWorld && b.Slug != "wiki")
            .Select(b => b.Slug)
            .ToList();
        chain.Add(result.Slug);
        return chain;
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
        text = HtmlTagRegex().Replace(text, " ");
        text = WorldLinkRegex().Replace(text, "$1");
        text = WhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\[\[[a-fA-F0-9\-]{36}(?:\|([^\]]+))?\]\]")]
    private static partial Regex WorldLinkRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

