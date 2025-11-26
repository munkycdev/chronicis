using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for synchronizing hashtags when articles are saved
/// WITH DEBUG LOGGING
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
        Console.WriteLine($"??? HashtagSyncService: Starting sync for article {articleId}");
        Console.WriteLine($"??? Body content: {body?.Substring(0, Math.Min(100, body?.Length ?? 0))}...");

        // Parse hashtags from the body
        var parsedHashtags = _parser.ExtractHashtags(body);

        Console.WriteLine($"??? Found {parsedHashtags.Count} hashtags: {string.Join(", ", parsedHashtags.Select(h => h.Name))}");

        // Get existing ArticleHashtag relationships for this article
        var existingRelations = await _context.ArticleHashtags
            .Include(ah => ah.Hashtag)
            .Where(ah => ah.ArticleId == articleId)
            .ToListAsync();

        Console.WriteLine($"??? Existing relations: {existingRelations.Count}");

        // Build a dictionary of current hashtag names for quick lookup
        var currentHashtagNames = parsedHashtags
            .Select(h => h.Name)
            .Distinct()
            .ToHashSet();

        // Remove hashtags that no longer exist in the body
        var relationsToRemove = existingRelations
            .Where(ah => !currentHashtagNames.Contains(ah.Hashtag.Name))
            .ToList();

        if (relationsToRemove.Any())
        {
            Console.WriteLine($"??? Removing {relationsToRemove.Count} old hashtag relations");
            _context.ArticleHashtags.RemoveRange(relationsToRemove);
        }

        // Get existing hashtag names from the relations
        var existingHashtagNames = existingRelations
            .Select(ah => ah.Hashtag.Name)
            .ToHashSet();

        // Add new hashtags
        foreach (var parsedHashtag in parsedHashtags)
        {
            Console.WriteLine($"??? Processing hashtag: {parsedHashtag.Name}");

            // Skip if this hashtag already exists for this article
            if (existingHashtagNames.Contains(parsedHashtag.Name))
            {
                Console.WriteLine($"???   -> Already exists, updating position");
                // Update position if needed
                var existingRelation = existingRelations
                    .FirstOrDefault(ah => ah.Hashtag.Name == parsedHashtag.Name);

                if (existingRelation != null)
                {
                    existingRelation.Position = parsedHashtag.Position;
                }
                continue;
            }

            // Check if hashtag exists globally
            var hashtag = await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Name == parsedHashtag.Name);

            // Create hashtag if it doesn't exist
            if (hashtag == null)
            {
                Console.WriteLine($"???   -> Creating new hashtag in database");
                hashtag = new Hashtag
                {
                    Name = parsedHashtag.Name,
                    LinkedArticleId = null,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Hashtags.Add(hashtag);
                await _context.SaveChangesAsync(); // Save to get the ID
                Console.WriteLine($"???   -> Created with ID: {hashtag.Id}");
            }
            else
            {
                Console.WriteLine($"???   -> Hashtag already exists with ID: {hashtag.Id}");
            }

            // Create the ArticleHashtag relationship
            Console.WriteLine($"???   -> Creating ArticleHashtag relation");
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
        Console.WriteLine($"??? HashtagSyncService: Sync complete for article {articleId}");
    }
}