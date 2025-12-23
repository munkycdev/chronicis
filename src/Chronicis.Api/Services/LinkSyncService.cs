using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Synchronizes wiki links in the database based on article content.
/// Uses a delete-then-insert strategy for simplicity.
/// </summary>
public class LinkSyncService : ILinkSyncService
{
    private readonly ChronicisDbContext _context;
    private readonly ILinkParser _linkParser;
    private readonly ILogger<LinkSyncService> _logger;

    public LinkSyncService(
        ChronicisDbContext context,
        ILinkParser linkParser,
        ILogger<LinkSyncService> logger)
    {
        _context = context;
        _linkParser = linkParser;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes the ArticleLink table for the given article.
    /// Removes all existing links for this article and creates new ones based on the body content.
    /// </summary>
    /// <param name="sourceArticleId">The ID of the article whose links should be synced.</param>
    /// <param name="body">The article body containing wiki links to parse.</param>
    public async Task SyncLinksAsync(Guid sourceArticleId, string? body)
    {
        // Step 1: Delete all existing links where this article is the source
        var existingLinks = await _context.ArticleLinks
            .Where(al => al.SourceArticleId == sourceArticleId)
            .ToListAsync();

        var removedCount = existingLinks.Count;
        
        if (existingLinks.Any())
        {
            _context.ArticleLinks.RemoveRange(existingLinks);
        }

        // Step 2: Parse new links from body
        var parsedLinks = _linkParser.ParseLinks(body);

        // Step 3: Create new ArticleLink entities
        var newLinks = parsedLinks.Select(pl => new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = sourceArticleId,
            TargetArticleId = pl.TargetArticleId,
            DisplayText = pl.DisplayText,
            Position = pl.Position,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var addedCount = newLinks.Count;

        if (newLinks.Any())
        {
            await _context.ArticleLinks.AddRangeAsync(newLinks);
        }

        // Step 4: Save all changes in a single transaction
        await _context.SaveChangesAsync();

        // Step 5: Log metrics
        _logger.LogInformation(
            "Synced links for article {ArticleId}: {RemovedCount} removed, {AddedCount} added",
            sourceArticleId,
            removedCount,
            addedCount);
    }
}
