using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for synchronizing hashtags when articles are saved.
/// </summary>
public class HashtagSyncService : IHashtagSyncService
{
    private readonly ChronicisDbContext _context;
    private readonly IHashtagParser _parser;

    public HashtagSyncService(ChronicisDbContext context, IHashtagParser parser)
    {
        _context = context;
        _parser = parser;
    }

    /// <summary>
    /// Synchronizes hashtags for an article based on its current body content.
    /// </summary>
    public async Task SyncHashtagsAsync(Guid articleId, string body)
    {
        var parsedHashtags = _parser.ExtractHashtags(body ?? string.Empty);

        var existingRelations = await _context.ArticleHashtags
            .Include(ah => ah.Hashtag)
            .Where(ah => ah.ArticleId == articleId)
            .ToListAsync();

        var currentHashtagNames = parsedHashtags
            .Select(h => h.Name)
            .Distinct()
            .ToHashSet();

        var relationsToRemove = existingRelations
            .Where(ah => !currentHashtagNames.Contains(ah.Hashtag.Name))
            .ToList();

        if (relationsToRemove.Any())
        {
            _context.ArticleHashtags.RemoveRange(relationsToRemove);
        }

        var existingHashtagNames = existingRelations
            .Select(ah => ah.Hashtag.Name)
            .ToHashSet();

        foreach (var parsedHashtag in parsedHashtags)
        {
            if (existingHashtagNames.Contains(parsedHashtag.Name))
            {
                var existingRelation = existingRelations
                    .FirstOrDefault(ah => ah.Hashtag.Name == parsedHashtag.Name);

                if (existingRelation != null)
                {
                    existingRelation.Position = parsedHashtag.Position;
                }
                continue;
            }

            var hashtag = await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Name == parsedHashtag.Name);

            if (hashtag == null)
            {
                // Auto-link: Try to find an article with a matching title (slug-normalized)
                var linkedArticleId = await FindMatchingArticleIdAsync(parsedHashtag.Name);

                hashtag = new Hashtag
                {
                    Id = Guid.NewGuid(),
                    Name = parsedHashtag.Name,
                    LinkedArticleId = linkedArticleId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Hashtags.Add(hashtag);
                await _context.SaveChangesAsync();
            }
            else if (hashtag.LinkedArticleId == null)
            {
                // Hashtag exists but isn't linked - try to auto-link it
                var linkedArticleId = await FindMatchingArticleIdAsync(parsedHashtag.Name);
                if (linkedArticleId != null)
                {
                    hashtag.LinkedArticleId = linkedArticleId;
                }
            }

            var articleHashtag = new ArticleHashtag
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                HashtagId = hashtag.Id,
                Position = parsedHashtag.Position,
                CreatedAt = DateTime.UtcNow
            };

            _context.ArticleHashtags.Add(articleHashtag);
        }

        await _context.SaveChangesAsync();

        // Also check if this article's title should link to any existing unlinked hashtags
        await LinkHashtagsToArticleByTitleAsync(articleId);
    }

    /// <summary>
    /// Attempts to auto-link any unlinked hashtags that match this article's title.
    /// Called when an article is created or updated.
    /// </summary>
    public async Task LinkHashtagsToArticleByTitleAsync(Guid articleId)
    {
        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null || string.IsNullOrWhiteSpace(article.Title))
            return;

        var slug = SlugGenerator.GenerateSlug(article.Title);

        // Find hashtags with this name that aren't linked yet
        var matchingHashtags = await _context.Hashtags
            .Where(h => h.Name == slug && h.LinkedArticleId == null)
            .ToListAsync();

        foreach (var hashtag in matchingHashtags)
        {
            hashtag.LinkedArticleId = articleId;
        }

        if (matchingHashtags.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Finds an article whose slug-normalized title matches the hashtag name.
    /// </summary>
    private async Task<Guid?> FindMatchingArticleIdAsync(string hashtagName)
    {
        // We need to fetch articles and compare slugs in-memory
        // because EF Core can't translate SlugUtility.CreateSlug to SQL
        var articles = await _context.Articles
            .Where(a => a.Title != null && a.Title != "")
            .Select(a => new { a.Id, a.Title })
            .ToListAsync();

        var matchingArticle = articles
            .FirstOrDefault(a => SlugGenerator.GenerateSlug(a.Title).Equals(
                hashtagName,
                StringComparison.OrdinalIgnoreCase));

        return matchingArticle?.Id;
    }
}
