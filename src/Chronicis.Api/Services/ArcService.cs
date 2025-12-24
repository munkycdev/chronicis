using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    public async Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId, Guid userId)
    {
        // Verify user has access to the campaign
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.OwnerId == userId);

        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for user {UserId}", campaignId, userId);
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
            .Where(a => a.Id == arcId && a.Campaign.OwnerId == userId)
            .Select(a => new ArcDto
            {
                Id = a.Id,
                CampaignId = a.CampaignId,
                Name = a.Name,
                Description = a.Description,
                SortOrder = a.SortOrder,
                SessionCount = _context.Articles.Count(art => art.ArcId == a.Id),
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.Creator.DisplayName
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ArcDto?> CreateArcAsync(ArcCreateDto dto, Guid userId)
    {
        // Verify user owns the campaign
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.CampaignId && c.OwnerId == userId);

        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for user {UserId}", dto.CampaignId, userId);
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

        _logger.LogInformation("Created arc {ArcId} '{ArcName}' in campaign {CampaignId}", 
            arc.Id, arc.Name, arc.CampaignId);

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            SortOrder = arc.SortOrder,
            SessionCount = 0,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy
        };
    }

    public async Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto, Guid userId)
    {
        var arc = await _context.Arcs
            .Include(a => a.Creator)
            .FirstOrDefaultAsync(a => a.Id == arcId && a.Campaign.OwnerId == userId);

        if (arc == null)
        {
            _logger.LogWarning("Arc {ArcId} not found for user {UserId}", arcId, userId);
            return null;
        }

        arc.Name = dto.Name;
        arc.Description = dto.Description;
        
        if (dto.SortOrder.HasValue)
        {
            arc.SortOrder = dto.SortOrder.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated arc {ArcId} '{ArcName}'", arc.Id, arc.Name);

        var sessionCount = await _context.Articles.CountAsync(a => a.ArcId == arcId);

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            SortOrder = arc.SortOrder,
            SessionCount = sessionCount,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy,
            CreatedByName = arc.Creator.DisplayName
        };
    }

    public async Task<bool> DeleteArcAsync(Guid arcId, Guid userId)
    {
        var arc = await _context.Arcs
            .FirstOrDefaultAsync(a => a.Id == arcId && a.Campaign.OwnerId == userId);

        if (arc == null)
        {
            _logger.LogWarning("Arc {ArcId} not found for user {UserId}", arcId, userId);
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

        _logger.LogInformation("Deleted arc {ArcId}", arcId);
        return true;
    }
}
