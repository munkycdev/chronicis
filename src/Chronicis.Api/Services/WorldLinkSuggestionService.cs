using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class WorldLinkSuggestionService : IWorldLinkSuggestionService
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleHierarchyService _hierarchyService;

    public WorldLinkSuggestionService(ChronicisDbContext context, IArticleHierarchyService hierarchyService)
    {
        _context = context;
        _hierarchyService = hierarchyService;
    }

    public async Task<ServiceResult<List<LinkSuggestionDto>>> GetSuggestionsAsync(Guid worldId, string query, Guid userId)
    {
        var hasAccess = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == userId);

        if (!hasAccess)
        {
            return ServiceResult<List<LinkSuggestionDto>>.Forbidden();
        }

        var normalizedQuery = query.ToLowerInvariant();

        var titleMatches = await _context.Articles
            .Where(a => a.WorldId == worldId)
            .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
            .Where(a => a.Title != null && a.Title.ToLower().Contains(normalizedQuery))
            .OrderBy(a => a.Title)
            .Take(20)
            .Select(a => new LinkSuggestionDto
            {
                ArticleId = a.Id,
                Title = a.Title ?? "Untitled",
                Slug = a.Slug,
                ArticleType = a.Type,
                DisplayPath = string.Empty,
                MatchedAlias = null
            })
            .ToListAsync();

        var titleMatchIds = titleMatches.Select(t => t.ArticleId).ToHashSet();

        var aliasMatches = await _context.ArticleAliases
            .Include(aa => aa.Article)
            .Where(aa => aa.Article.WorldId == worldId)
            .Where(aa => aa.Article.Type != ArticleType.Tutorial && aa.Article.WorldId != Guid.Empty)
            .Where(aa => aa.AliasText.ToLower().Contains(normalizedQuery))
            .Where(aa => !titleMatchIds.Contains(aa.ArticleId))
            .OrderBy(aa => aa.AliasText)
            .Take(20)
            .Select(aa => new LinkSuggestionDto
            {
                ArticleId = aa.ArticleId,
                Title = aa.Article.Title ?? "Untitled",
                Slug = aa.Article.Slug,
                ArticleType = aa.Article.Type,
                DisplayPath = string.Empty,
                MatchedAlias = aa.AliasText
            })
            .ToListAsync();

        var suggestions = titleMatches
            .Concat(aliasMatches)
            .Take(20)
            .ToList();

        foreach (var suggestion in suggestions)
        {
            suggestion.DisplayPath = await _hierarchyService.BuildDisplayPathAsync(suggestion.ArticleId);
        }

        return ServiceResult<List<LinkSuggestionDto>>.Success(suggestions);
    }
}

