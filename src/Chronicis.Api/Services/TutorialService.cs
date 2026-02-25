using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class TutorialService : ITutorialService
{
    private const string DefaultPageType = "Page:Default";
    private const string ArticleTypePrefix = "ArticleType:";
    private const string ArticleTypeAny = "ArticleType:Any";

    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TutorialService> _logger;

    public TutorialService(
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<TutorialService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TutorialDto?> ResolveAsync(string pageType)
    {
        var normalizedPageType = string.IsNullOrWhiteSpace(pageType)
            ? DefaultPageType
            : pageType.Trim();

        var exact = await TryResolveByPageTypeAsync(normalizedPageType);
        if (exact != null)
        {
            return exact;
        }

        if (normalizedPageType.StartsWith(ArticleTypePrefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedPageType, ArticleTypeAny, StringComparison.OrdinalIgnoreCase))
        {
            var anyArticleType = await TryResolveByPageTypeAsync(ArticleTypeAny);
            if (anyArticleType != null)
            {
                return anyArticleType;
            }
        }

        return await TryResolveByPageTypeAsync(DefaultPageType);
    }

    public async Task<List<TutorialMappingDto>> GetMappingsAsync()
    {
        await ThrowIfNotSysAdminAsync();

        return await _context.TutorialPages
            .AsNoTracking()
            .Where(tp => tp.Article.Type == ArticleType.Tutorial && tp.Article.WorldId == Guid.Empty)
            .Select(tp => new TutorialMappingDto
            {
                Id = tp.Id,
                PageType = tp.PageType,
                PageTypeName = tp.PageTypeName,
                ArticleId = tp.ArticleId,
                Title = tp.Article.Title,
                ModifiedAt = tp.Article.ModifiedAt ?? tp.Article.CreatedAt
            })
            .OrderBy(tp => tp.PageType)
            .ToListAsync();
    }

    public async Task<TutorialMappingDto> CreateMappingAsync(TutorialMappingCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await ThrowIfNotSysAdminAsync();

        var pageType = NormalizeRequired(dto.PageType, nameof(dto.PageType));
        var pageTypeName = NormalizeRequired(dto.PageTypeName, nameof(dto.PageTypeName));

        var pageTypeExists = await _context.TutorialPages
            .AnyAsync(tp => tp.PageType == pageType);
        if (pageTypeExists)
        {
            throw new InvalidOperationException($"A tutorial mapping already exists for page type '{pageType}'.");
        }

        var now = DateTime.UtcNow;
        Article article;

        if (dto.ArticleId.HasValue)
        {
            article = await GetRequiredTutorialArticleAsync(dto.ArticleId.Value);

            // Normalize legacy/invalid tutorial rows to the system world sentinel.
            if (article.WorldId != Guid.Empty)
            {
                article.WorldId = Guid.Empty;
            }
        }
        else
        {
            var user = await _currentUserService.GetRequiredUserAsync();
            var title = string.IsNullOrWhiteSpace(dto.Title)
                ? pageTypeName
                : dto.Title.Trim();
            var slug = await GenerateUniqueTutorialSlugAsync(title, parentId: null, excludeArticleId: null);

            article = new Article
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = slug,
                Body = dto.Body ?? string.Empty,
                Type = ArticleType.Tutorial,
                Visibility = ArticleVisibility.Public,
                WorldId = Guid.Empty,
                CreatedBy = user.Id,
                CreatedAt = now,
                EffectiveDate = now
            };

            _context.Articles.Add(article);
        }

        var mapping = new TutorialPage
        {
            Id = Guid.NewGuid(),
            PageType = pageType,
            PageTypeName = pageTypeName,
            ArticleId = article.Id,
            CreatedAt = now,
            ModifiedAt = now
        };

        _context.TutorialPages.Add(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "SysAdmin created tutorial mapping {PageType} -> article {ArticleId}",
            mapping.PageType,
            mapping.ArticleId);

        return new TutorialMappingDto
        {
            Id = mapping.Id,
            PageType = mapping.PageType,
            PageTypeName = mapping.PageTypeName,
            ArticleId = mapping.ArticleId,
            Title = article.Title,
            ModifiedAt = article.ModifiedAt ?? article.CreatedAt
        };
    }

    public async Task<TutorialMappingDto?> UpdateMappingAsync(Guid id, TutorialMappingUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await ThrowIfNotSysAdminAsync();

        var pageType = NormalizeRequired(dto.PageType, nameof(dto.PageType));
        var pageTypeName = NormalizeRequired(dto.PageTypeName, nameof(dto.PageTypeName));

        var mapping = await _context.TutorialPages
            .FirstOrDefaultAsync(tp => tp.Id == id);
        if (mapping == null)
        {
            return null;
        }

        var duplicatePageType = await _context.TutorialPages
            .AnyAsync(tp => tp.Id != id && tp.PageType == pageType);
        if (duplicatePageType)
        {
            throw new InvalidOperationException($"A tutorial mapping already exists for page type '{pageType}'.");
        }

        var article = await GetRequiredTutorialArticleAsync(dto.ArticleId);
        if (article.WorldId != Guid.Empty)
        {
            article.WorldId = Guid.Empty;
        }

        mapping.PageType = pageType;
        mapping.PageTypeName = pageTypeName;
        mapping.ArticleId = article.Id;
        mapping.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "SysAdmin updated tutorial mapping {MappingId} ({PageType}) -> article {ArticleId}",
            mapping.Id,
            mapping.PageType,
            mapping.ArticleId);

        return new TutorialMappingDto
        {
            Id = mapping.Id,
            PageType = mapping.PageType,
            PageTypeName = mapping.PageTypeName,
            ArticleId = mapping.ArticleId,
            Title = article.Title,
            ModifiedAt = article.ModifiedAt ?? article.CreatedAt
        };
    }

    public async Task<bool> DeleteMappingAsync(Guid id)
    {
        await ThrowIfNotSysAdminAsync();

        var mapping = await _context.TutorialPages
            .FirstOrDefaultAsync(tp => tp.Id == id);
        if (mapping == null)
        {
            return false;
        }

        _context.TutorialPages.Remove(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SysAdmin deleted tutorial mapping {MappingId}", id);
        return true;
    }

    private async Task<TutorialDto?> TryResolveByPageTypeAsync(string pageType)
    {
        return await _context.TutorialPages
            .AsNoTracking()
            .Where(tp => tp.PageType == pageType)
            .Where(tp => tp.Article.Type == ArticleType.Tutorial && tp.Article.WorldId == Guid.Empty)
            .Select(tp => new TutorialDto
            {
                ArticleId = tp.ArticleId,
                Title = tp.Article.Title,
                Body = tp.Article.Body ?? string.Empty,
                ModifiedAt = tp.Article.ModifiedAt ?? tp.Article.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    private async Task ThrowIfNotSysAdminAsync()
    {
        if (!await _currentUserService.IsSysAdminAsync())
        {
            throw new UnauthorizedAccessException("Caller is not a system administrator.");
        }
    }

    private async Task<Article> GetRequiredTutorialArticleAsync(Guid articleId)
    {
        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
        {
            throw new InvalidOperationException($"Tutorial article {articleId} was not found.");
        }

        if (article.Type != ArticleType.Tutorial)
        {
            throw new InvalidOperationException(
                $"Article {articleId} is not a tutorial article and cannot be mapped.");
        }

        return article;
    }

    private async Task<string> GenerateUniqueTutorialSlugAsync(string title, Guid? parentId, Guid? excludeArticleId)
    {
        var baseSlug = SlugGenerator.GenerateSlug(title);

        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Type == ArticleType.Tutorial && a.WorldId == Guid.Empty);

        if (parentId.HasValue)
        {
            query = query.Where(a => a.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(a => a.ParentId == null);
        }

        if (excludeArticleId.HasValue)
        {
            query = query.Where(a => a.Id != excludeArticleId.Value);
        }

        var existingSlugs = await query
            .Select(a => a.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
    }

    private static string NormalizeRequired(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }
}
