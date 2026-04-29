using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public sealed class ArcService : IArcService
{
    private readonly ChronicisDbContext _context;
    private readonly IReservedSlugProvider _reservedSlugProvider;
    private readonly ILogger<ArcService> _logger;

    public ArcService(
        ChronicisDbContext context,
        IReservedSlugProvider reservedSlugProvider,
        ILogger<ArcService> logger)
    {
        _context = context;
        _reservedSlugProvider = reservedSlugProvider;
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

    /// <summary>
    /// Check if user is a GM in the world that owns the campaign.
    /// </summary>
    private async Task<bool> UserIsCampaignGMAsync(Guid campaignId, Guid userId)
    {
        return await _context.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.World.Members.Any(m => m.UserId == userId && m.Role == WorldRole.GM));
    }

    /// <summary>
    /// Check if user is either the world owner or a GM for the campaign.
    /// </summary>
    private async Task<bool> UserIsCampaignOwnerOrGMAsync(Guid campaignId, Guid userId)
    {
        return await _context.Campaigns
            .AnyAsync(c => c.Id == campaignId && (c.World.OwnerId == userId
                || c.World.Members.Any(m => m.UserId == userId && m.Role == WorldRole.GM)));
    }

    public async Task<List<ArcDto>> GetArcsByCampaignAsync(Guid campaignId, Guid userId)
    {
        // Verify user has access to the campaign via world membership
        if (!await UserHasCampaignAccessAsync(campaignId, userId))
        {
            _logger.LogWarningSanitized("Campaign {CampaignId} not found or user {UserId} doesn't have access", campaignId, userId);
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
                PrivateNotes = null,
                SortOrder = a.SortOrder,
                SessionCount = _context.Articles.Count(art => art.ArcId == a.Id),
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.Creator.DisplayName,
                Slug = a.Slug,
                CampaignSlug = a.Campaign.Slug,
                WorldSlug = a.Campaign.World.Slug
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
                PrivateNotes = a.Campaign.World.OwnerId == userId
                    || a.Campaign.World.Members.Any(m => m.UserId == userId && m.Role == WorldRole.GM)
                    ? a.PrivateNotes
                    : null,
                SortOrder = a.SortOrder,
                SessionCount = _context.Articles.Count(art => art.ArcId == a.Id),
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.Creator.DisplayName,
                Slug = a.Slug,
                CampaignSlug = a.Campaign.Slug,
                WorldSlug = a.Campaign.World.Slug
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ArcDto?> CreateArcAsync(ArcCreateDto dto, Guid userId)
    {
        // Only GMs can create arcs
        if (!await UserIsCampaignGMAsync(dto.CampaignId, userId))
        {
            _logger.LogWarningSanitized("Campaign {CampaignId} not found or user {UserId} is not a GM", dto.CampaignId, userId);
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

        var arcSlugBase = !string.IsNullOrWhiteSpace(dto.Slug) && SlugGenerator.IsValidSlug(dto.Slug.Trim())
            ? dto.Slug.Trim()
            : SlugGenerator.GenerateSlug(dto.Name);
        var existingArcSlugs = await _context.Arcs.AsNoTracking()
            .Where(a => a.CampaignId == dto.CampaignId)
            .Select(a => a.Slug)
            .ToHashSetAsync();
        var arcSlug = SlugGenerator.GenerateUniqueSiblingSlug(arcSlugBase, existingArcSlugs, _reservedSlugProvider.All);
        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = dto.CampaignId,
            Name = dto.Name,
            Slug = arcSlug,
            Description = dto.Description,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Arcs.Add(arc);
        await _context.SaveChangesAsync();

        _logger.LogTraceSanitized("Created arc {ArcId} '{ArcName}' in campaign {CampaignId}",
            arc.Id, arc.Name, arc.CampaignId);

        var campaignSlugInfo = await _context.Campaigns.AsNoTracking()
            .Where(c => c.Id == arc.CampaignId)
            .Select(c => new { c.Slug, WorldSlug = c.World.Slug })
            .FirstOrDefaultAsync();

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            PrivateNotes = null,
            SortOrder = arc.SortOrder,
            SessionCount = 0,
            IsActive = arc.IsActive,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy,
            Slug = arc.Slug,
            CampaignSlug = campaignSlugInfo?.Slug ?? string.Empty,
            WorldSlug = campaignSlugInfo?.WorldSlug ?? string.Empty
        };
    }

    public async Task<ArcDto?> UpdateArcAsync(Guid arcId, ArcUpdateDto dto, Guid userId)
    {
        var arc = await _context.Arcs
            .Include(a => a.Creator)
            .Include(a => a.Campaign)
                .ThenInclude(c => c.World)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            _logger.LogWarningSanitized("Arc {ArcId} not found", arcId);
            return null;
        }

        if (!await UserIsCampaignOwnerOrGMAsync(arc.CampaignId, userId))
        {
            _logger.LogWarningSanitized("User {UserId} is not authorized to update arc {ArcId}", userId, arcId);
            return null;
        }

        if (arc.Name != dto.Name)
        {
            arc.Slug = await GenerateUniqueArcSlugAsync(dto.Name, arc.CampaignId, arc.Id);
        }

        arc.Name = dto.Name;
        arc.Description = dto.Description;
        arc.PrivateNotes = string.IsNullOrWhiteSpace(dto.PrivateNotes) ? null : dto.PrivateNotes;

        if (dto.SortOrder.HasValue)
        {
            arc.SortOrder = dto.SortOrder.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogTraceSanitized("Updated arc {ArcId} '{ArcName}'", arc.Id, arc.Name);

        var sessionCount = await _context.Articles.CountAsync(a => a.ArcId == arcId);

        return new ArcDto
        {
            Id = arc.Id,
            CampaignId = arc.CampaignId,
            Name = arc.Name,
            Description = arc.Description,
            PrivateNotes = arc.PrivateNotes,
            SortOrder = arc.SortOrder,
            SessionCount = sessionCount,
            IsActive = arc.IsActive,
            CreatedAt = arc.CreatedAt,
            CreatedBy = arc.CreatedBy,
            CreatedByName = arc.Creator.DisplayName,
            Slug = arc.Slug,
            CampaignSlug = arc.Campaign?.Slug ?? string.Empty,
            WorldSlug = arc.Campaign?.World?.Slug ?? string.Empty
        };
    }

    public async Task<bool> DeleteArcAsync(Guid arcId, Guid userId)
    {
        var arc = await _context.Arcs
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            _logger.LogWarningSanitized("Arc {ArcId} not found", arcId);
            return false;
        }

        if (!await UserIsCampaignGMAsync(arc.CampaignId, userId))
        {
            _logger.LogWarningSanitized("User {UserId} is not a GM for arc {ArcId}", userId, arcId);
            return false;
        }

        // Check if arc has sessions
        var hasContent = await _context.Articles.AnyAsync(a => a.ArcId == arcId);
        if (hasContent)
        {
            _logger.LogWarningSanitized("Cannot delete arc {ArcId} - it has sessions", arcId);
            return false;
        }

        _context.Arcs.Remove(arc);
        await _context.SaveChangesAsync();

        _logger.LogTraceSanitized("Deleted arc {ArcId}", arcId);
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

        // Only the world owner or GMs can activate arcs
        if (!(arc.Campaign.World.OwnerId == userId
            || arc.Campaign.World.Members.Any(m => m.UserId == userId && m.Role == WorldRole.GM)))
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

        _logger.LogTraceSanitized("Activated arc {ArcId} in campaign {CampaignId}", arcId, arc.CampaignId);

        return true;
    }

    public async Task<(Guid Id, string Name)?> GetIdBySlugAsync(Guid campaignId, string slug)
    {
        var row = await _context.Arcs.AsNoTracking()
            .Where(a => a.CampaignId == campaignId && a.Slug == slug)
            .Select(a => new { a.Id, a.Name })
            .FirstOrDefaultAsync();

        return row == null ? null : (row.Id, row.Name);
    }

    public async Task<ServiceResult<string>> UpdateSlugAsync(Guid arcId, string slug, Guid userId)
    {
        var arc = await _context.Arcs.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
            return ServiceResult<string>.NotFound("Arc not found");

        if (!await UserIsCampaignOwnerOrGMAsync(arc.CampaignId, userId))
            return ServiceResult<string>.Forbidden("Only the world owner or GM may update the slug");

        if (!SlugGenerator.IsValidSlug(slug))
            return ServiceResult<string>.ValidationError("SLUG_INVALID");

        if (_reservedSlugProvider.IsReserved(slug))
            return ServiceResult<string>.ValidationError("SLUG_RESERVED");

        var existing = await _context.Arcs.AsNoTracking()
            .Where(a => a.CampaignId == arc.CampaignId && a.Id != arcId)
            .Select(a => a.Slug)
            .ToHashSetAsync();

        var finalSlug = SlugGenerator.GenerateUniqueSiblingSlug(slug, existing, _reservedSlugProvider.All);

        var tracked = await _context.Arcs.FirstAsync(a => a.Id == arcId);
        tracked.Slug = finalSlug;
        await _context.SaveChangesAsync();

        return ServiceResult<string>.Success(finalSlug);
    }

    private async Task<string> GenerateUniqueArcSlugAsync(string name, Guid campaignId, Guid? excludeId = null)
    {
        var existing = await _context.Arcs.AsNoTracking()
            .Where(a => a.CampaignId == campaignId && (!excludeId.HasValue || a.Id != excludeId.Value))
            .Select(a => a.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSiblingSlug(
            SlugGenerator.GenerateSlug(name), existing, _reservedSlugProvider.All);
    }
}
