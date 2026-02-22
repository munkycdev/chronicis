using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Implementation of system administrator operations.
/// Every public method verifies sysadmin status via <see cref="ICurrentUserService"/>
/// before performing any work.
/// </summary>
public class AdminService : IAdminService
{
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AdminService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<AdminWorldSummaryDto>> GetAllWorldSummariesAsync()
    {
        await ThrowIfNotSysAdminAsync();

        _logger.LogDebug("SysAdmin fetching all world summaries");

        return await BuildWorldSummaryQueryAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteWorldAsync(Guid worldId)
    {
        await ThrowIfNotSysAdminAsync();

        var world = await _context.Worlds.FindAsync(worldId);
        if (world == null)
        {
            _logger.LogDebug("SysAdmin delete: world {WorldId} not found", worldId);
            return false;
        }

        _logger.LogWarning("SysAdmin permanently deleting world {WorldId} ({WorldName})",
            worldId, world.Name);

        await DeleteWorldDataAsync(worldId);

        _context.Worlds.Remove(world);
        await _context.SaveChangesAsync();

        _logger.LogWarning("SysAdmin permanently deleted world {WorldId}", worldId);
        return true;
    }

    // ────────────────────────────────────────────────────────────────
    //  Private helpers
    // ────────────────────────────────────────────────────────────────

    private async Task ThrowIfNotSysAdminAsync()
    {
        if (!await _currentUserService.IsSysAdminAsync())
            throw new UnauthorizedAccessException("Caller is not a system administrator.");
    }

    /// <summary>
    /// Builds the world summary query using a single set of aggregate subqueries,
    /// avoiding N+1 correlated queries per world.
    /// </summary>
    internal async Task<List<AdminWorldSummaryDto>> BuildWorldSummaryQueryAsync()
    {
        var summaries = await _context.Worlds
            .AsNoTracking()
            .Select(w => new AdminWorldSummaryDto
            {
                Id = w.Id,
                Name = w.Name,
                OwnerName = w.Owner != null ? w.Owner.DisplayName : "Unknown",
                OwnerEmail = w.Owner != null ? w.Owner.Email : string.Empty,
                CampaignCount = w.Campaigns.Count,
                ArcCount = w.Campaigns.SelectMany(c => c.Arcs).Count(),
                ArticleCount = w.Articles.Count,
                CreatedAt = w.CreatedAt,
            })
            .OrderBy(s => s.Name)
            .ToListAsync();

        return summaries;
    }


    /// <summary>
    /// Deletes world-owned data in an order that satisfies FK constraints.
    /// EF cascade handles: WorldMembers, WorldInvitations, WorldDocuments,
    /// WorldLinks, WorldResourceProviders, SummaryTemplates, Arcs→Quests→QuestUpdates.
    /// We must handle manually: ArticleLinks (NoAction FK), then Articles,
    /// then Campaigns (Restrict FK to World).
    /// </summary>
    private async Task DeleteWorldDataAsync(Guid worldId)
    {
        // 1. Article links — target FK is NoAction; delete before articles
        var articleIds = _context.Articles
            .Where(a => a.WorldId == worldId)
            .Select(a => a.Id);

        var incomingLinks = _context.ArticleLinks
            .Where(al => articleIds.Contains(al.TargetArticleId));
        _context.ArticleLinks.RemoveRange(incomingLinks);
        await _context.SaveChangesAsync();

        // 2. Articles — Restrict FK to World; must remove before world
        var articles = _context.Articles.Where(a => a.WorldId == worldId);
        _context.Articles.RemoveRange(articles);
        await _context.SaveChangesAsync();

        // 3. Campaigns — Restrict FK to World; Arcs/Quests/QuestUpdates cascade
        var campaigns = _context.Campaigns.Where(c => c.WorldId == worldId);
        _context.Campaigns.RemoveRange(campaigns);
        await _context.SaveChangesAsync();
    }
}
