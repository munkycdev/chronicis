using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class ArcService : IArcService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ArcService> _logger;

    public ArcService(ChronicisDbContext context, ILogger<ArcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has access to a campaign (via world membership).
    /// </summary>
    private async Task<bool> UserHasCampaignAccessAsync(Guid campaignId, Guid userId)
    {
        return await _context.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.World.Members.Any(m => m.UserId == userId));
    }

    public async Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId, Guid userId)
    {
        // Verify user has access to the campaign via world membership
        if (!await UserHasCampaignAccessAsync(campaignId, userId))
        {
            _logger.LogWarning("Campaign {CampaignId} not found or user {UserId} doesn't have access", campaignId, userId);
            return new List<ArcDto>();
        }

        return await _context.Arcs
            .AsNoTracking()
            .Where(a => a.CampaignId == campaignId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new ArcDto
            {
                Id = a.Id,
                CampaignId = a.CampaignId,
                Name = a.Name,
                Description = a.Description,
                SortOrder = a.SortOrder,
                SessionCount = _context.Articles.Count(art => art.ArcId == a.Id),
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.Creator.DisplayName
            })
            .ToListAsync();
    }

    public async Task<ArcDto?> GetArcAsync(Guid arcId, Guid userId)
    {
        return await _context.Arcs
            .AsNoTracking()
            .Where(a => a.Id == arcId && a.Campaign.World.Members.Any(m => m.UserId == userId))
            .Select(a => new ArcDto
            {
                Id = a.Id,
                CampaignId = a.CampaignId,
                Name = a.Name,
                Description = a.Description,
                SortOrder = a.SortOrder,
                SessionCount = _context.Articles.Count(art => art.ArcId == a.Id),
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.Creator.DisplayName
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ArcDto?> CreateArcAsync(ArcCreateDto dto, Guid userId)
    {
        // Verify user has access to the campaign via world membership
        if (!await UserHasCampaignAccessAsync(dto.CampaignId, userId))
        {
            _logger.LogWarning("Campaign {CampaignId} not found or user {UserId} doesn't have access", dto.CampaignId, userId);
            return null;
        }

        // Use provided sort order or auto-calculate next
        var sortOrder = dto.SortOrder;
        if (sortOrder == 0)
        {
            var maxSortOrder = await _context.Arcs
                .Where(a => a.CampaignId == dto.CampaignId)
                .MaxAsync(a => (int?)a.SortOrder) ?? 0;
            sortOrder = maxSortOrder + 1;
        }

        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = dto.CampaignId,
            Name = dto.Name,
            Description = dto.Description,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Arcs.Add(arc);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created arc {ArcId} '{ArcName}' in campaign {CampaignId}",
            arc.Id, arc.Name, arc.CampaignId);

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            SortOrder = arc.SortOrder,
            SessionCount = 0,
            IsActive = arc.IsActive,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy
        };
    }

    public async Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto, Guid userId)
    {
        var arc = await _context.Arcs
            .Include(a => a.Creator)
            .FirstOrDefaultAsync(a => a.Id == arcId && a.Campaign.World.Members.Any(m => m.UserId == userId));

        if (arc == null)
        {
            _logger.LogWarning("Arc {ArcId} not found or user {UserId} doesn't have access", arcId, userId);
            return null;
        }

        arc.Name = dto.Name;
        arc.Description = dto.Description;

        if (dto.SortOrder.HasValue)
        {
            arc.SortOrder = dto.SortOrder.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated arc {ArcId} '{ArcName}'", arc.Id, arc.Name);

        var sessionCount = await _context.Articles.CountAsync(a => a.ArcId == arcId);

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            SortOrder = arc.SortOrder,
            SessionCount = sessionCount,
            IsActive = arc.IsActive,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy,
            CreatedByName = arc.Creator.DisplayName
        };
    }

    public async Task<bool> DeleteArcAsync(Guid arcId, Guid userId)
    {
        var arc = await _context.Arcs
            .FirstOrDefaultAsync(a => a.Id == arcId && a.Campaign.World.Members.Any(m => m.UserId == userId));

        if (arc == null)
        {
            _logger.LogWarning("Arc {ArcId} not found or user {UserId} doesn't have access", arcId, userId);
            return false;
        }

        // Check if arc has sessions
        var hasContent = await _context.Articles.AnyAsync(a => a.ArcId == arcId);
        if (hasContent)
        {
            _logger.LogWarning("Cannot delete arc {ArcId} - it has sessions", arcId);
            return false;
        }

        _context.Arcs.Remove(arc);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Deleted arc {ArcId}", arcId);
        return true;
    }

    public async Task<bool> ActivateArcAsync(Guid arcId, Guid userId)
    {
        var arc = await _context.Arcs
            .Include(a => a.Campaign)
                .ThenInclude(c => c.World)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
            return false;

        // User must be a member of the world to activate arcs
        if (!arc.Campaign.World.Members.Any(m => m.UserId == userId))
            return false;

        // Deactivate all arcs in the same campaign
        var campaignArcs = await _context.Arcs
            .Where(a => a.CampaignId == arc.CampaignId && a.IsActive)
            .ToListAsync();

        foreach (var a in campaignArcs)
        {
            a.IsActive = false;
        }

        // Activate this arc
        arc.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Activated arc {ArcId} in campaign {CampaignId}", arcId, arc.CampaignId);

        return true;
    }
}
