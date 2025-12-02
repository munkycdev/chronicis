using Chronicis.Api.Data;
using Chronicis.Shared.Models;
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
    public async Task SyncHashtagsAsync(int articleId, string body)
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
                hashtag = new Hashtag
                {
                    Name = parsedHashtag.Name,
                    LinkedArticleId = null,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Hashtags.Add(hashtag);
                await _context.SaveChangesAsync();
            }

            var articleHashtag = new ArticleHashtag
            {
                ArticleId = articleId,
                HashtagId = hashtag.Id,
                Position = parsedHashtag.Position,
                CreatedDate = DateTime.UtcNow
            };

            _context.ArticleHashtags.Add(articleHashtag);
        }

        await _context.SaveChangesAsync();
    }
}
