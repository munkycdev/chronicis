using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class ArticleDataAccessService : IArticleDataAccessService
{
    private readonly ChronicisDbContext _context;
    private readonly IWorldDocumentService _worldDocumentService;

    public ArticleDataAccessService(ChronicisDbContext context, IWorldDocumentService worldDocumentService)
    {
        _context = context;
        _worldDocumentService = worldDocumentService;
    }

    public async Task AddArticleAsync(Article article)
    {
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task<Article?> FindReadableArticleAsync(Guid articleId, Guid userId)
    {
        return await GetReadableArticlesQuery(userId)
            .Where(a => a.Id == articleId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsTutorialSlugUniqueAsync(string slug, Guid? parentId, Guid? excludeArticleId = null)
    {
        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty && a.Slug == slug);

        query = parentId.HasValue
            ? query.Where(a => a.ParentId == parentId.Value)
            : query.Where(a => a.ParentId == null);

        if (excludeArticleId.HasValue)
        {
            query = query.Where(a => a.Id != excludeArticleId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<string> GenerateTutorialSlugAsync(string title, Guid? parentId, Guid? excludeArticleId = null)
    {
        var baseSlug = SlugGenerator.GenerateSlug(title);

        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);

        query = parentId.HasValue
            ? query.Where(a => a.ParentId == parentId.Value)
            : query.Where(a => a.ParentId == null);

        if (excludeArticleId.HasValue)
        {
            query = query.Where(a => a.Id != excludeArticleId.Value);
        }

        var existingSlugs = await query
            .Select(a => a.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
    }

    public async Task DeleteArticleAndDescendantsAsync(Guid articleId)
    {
        var children = await _context.Articles
            .Where(a => a.ParentId == articleId)
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var childId in children)
        {
            await DeleteArticleAndDescendantsAsync(childId);
        }

        var linksToDelete = await _context.ArticleLinks
            .Where(l => l.SourceArticleId == articleId || l.TargetArticleId == articleId)
            .ToListAsync();
        _context.ArticleLinks.RemoveRange(linksToDelete);

        await _worldDocumentService.DeleteArticleImagesAsync(articleId);

        var article = await _context.Articles.FindAsync(articleId);
        if (article != null)
        {
            _context.Articles.Remove(article);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId)
    {
        return await _context.ArticleLinks
            .Where(l => l.TargetArticleId == articleId)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.SourceArticleId,
                Title = l.SourceArticle.Title ?? "Untitled",
                Slug = l.SourceArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = string.Empty
            })
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId)
    {
        return await _context.ArticleLinks
            .Where(l => l.SourceArticleId == articleId)
            .Select(l => new BacklinkDto
            {
                ArticleId = l.TargetArticleId,
                Title = l.TargetArticle.Title ?? "Untitled",
                Slug = l.TargetArticle.Slug,
                Snippet = l.DisplayText,
                DisplayPath = string.Empty
            })
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<ResolvedLinkDto>> ResolveReadableLinksAsync(IEnumerable<Guid> articleIds, Guid userId)
    {
        var requested = articleIds.ToList();
        return await GetReadableArticlesQuery(userId)
            .Where(a => requested.Contains(a.Id))
            .Select(a => new ResolvedLinkDto
            {
                ArticleId = a.Id,
                Exists = true,
                Title = a.Title,
                Slug = a.Slug
            })
            .ToListAsync();
    }

    public async Task<(bool Found, Guid? WorldId)> TryGetReadableArticleWorldAsync(Guid articleId, Guid userId)
    {
        var article = await GetReadableArticlesQuery(userId)
            .Where(a => a.Id == articleId)
            .Select(a => new { a.WorldId })
            .FirstOrDefaultAsync();

        return article == null
            ? (false, null)
            : (true, article.WorldId);
    }

    public async Task<Article?> GetReadableArticleWithAliasesAsync(Guid articleId, Guid userId)
    {
        return await _context.Articles
            .Include(a => a.Aliases)
            .Where(a => a.Id == articleId)
            .Where(a => (a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty) ||
                        (a.World != null && a.World.Members.Any(m => m.UserId == userId)))
            .FirstOrDefaultAsync();
    }

    public async Task UpsertAliasesAsync(Article article, IReadOnlyCollection<string> newAliases, Guid userId)
    {
        var aliasesToRemove = article.Aliases
            .Where(existing => !newAliases.Contains(existing.AliasText, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var alias in aliasesToRemove)
        {
            _context.ArticleAliases.Remove(alias);
        }

        var existingAliasTexts = article.Aliases
            .Select(a => a.AliasText.ToLowerInvariant())
            .ToHashSet();

        foreach (var aliasText in newAliases)
        {
            if (!existingAliasTexts.Contains(aliasText.ToLowerInvariant()))
            {
                _context.ArticleAliases.Add(new ArticleAlias
                {
                    Id = Guid.NewGuid(),
                    ArticleId = article.Id,
                    AliasText = aliasText,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        article.ModifiedAt = DateTime.UtcNow;
        article.LastModifiedBy = userId;

        await _context.SaveChangesAsync();
    }

    private IQueryable<Article> GetReadableArticlesQuery(Guid userId)
    {
        var worldScoped = _context.Articles
            .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId));

        var tutorials = _context.Articles
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);

        return worldScoped.Concat(tutorials);
    }
}

