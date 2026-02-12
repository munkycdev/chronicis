using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for campaign management
/// </summary>
public class CampaignService : ICampaignService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(ChronicisDbContext context, ILogger<CampaignService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CampaignDetailDto?> GetCampaignAsync(Guid campaignId, Guid userId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Owner)
            .Include(c => c.Arcs)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return null;

        // Check access via world membership
        if (!await UserHasAccessAsync(campaignId, userId))
            return null;

        return MapToDetailDto(campaign);
    }

    public async Task<CampaignDto> CreateCampaignAsync(CampaignCreateDto dto, Guid userId)
    {
        // Verify user is GM in the world
        var world = await _context.Worlds.FindAsync(dto.WorldId);
        if (world == null)
            throw new InvalidOperationException("World not found");

        var isGM = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == dto.WorldId && wm.UserId == userId && wm.Role == WorldRole.GM);

        if (!isGM)
            throw new UnauthorizedAccessException("Only GMs can create campaigns");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        _logger.LogDebug("Creating campaign '{Name}' in world {WorldId} for user {UserId}",
            dto.Name, dto.WorldId, userId);

        // Create the Campaign entity
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = dto.WorldId,
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Campaigns.Add(campaign);

        // Create a default Arc (Act 1)
        var defaultArc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Name = "Act 1",
            Description = null,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        _context.Arcs.Add(defaultArc);

        await _context.SaveChangesAsync();

        _logger.LogDebug("Created campaign {CampaignId} with default Arc for user {UserId}",
            campaign.Id, userId);

        // Return DTO
        campaign.Owner = user;
        return MapToDto(campaign);
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto, Guid userId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return null;

        // Only GM can update
        if (!await UserIsGMAsync(campaignId, userId))
            return null;

        campaign.Name = dto.Name;
        campaign.Description = dto.Description;
        campaign.StartedAt = dto.StartedAt;
        campaign.EndedAt = dto.EndedAt;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated campaign {CampaignId}", campaignId);

        return MapToDto(campaign);
    }

    public async Task<WorldRole?> GetUserRoleAsync(Guid campaignId, Guid userId)
    {
        // Get the world for this campaign, then check world membership
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
            return null;

        var member = await _context.WorldMembers
            .FirstOrDefaultAsync(wm => wm.WorldId == campaign.WorldId && wm.UserId == userId);

        return member?.Role;
    }

    public async Task<bool> UserHasAccessAsync(Guid campaignId, Guid userId)
    {
        // User has access if they're a member of the campaign's world
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
            return false;

        return await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == campaign.WorldId && wm.UserId == userId);
    }

    public async Task<bool> UserIsGMAsync(Guid campaignId, Guid userId)
    {
        // User is GM if they have GM role in the campaign's world
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
            return false;

        return await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == campaign.WorldId
                        && wm.UserId == userId
                        && wm.Role == WorldRole.GM);
    }

    public async Task<bool> ActivateCampaignAsync(Guid campaignId, Guid userId)
    {
        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return false;

        // Only GM can activate
        if (!await UserIsGMAsync(campaignId, userId))
            return false;

        // Deactivate all campaigns in the same world
        var worldCampaigns = await _context.Campaigns
            .Where(c => c.WorldId == campaign.WorldId && c.IsActive)
            .ToListAsync();

        foreach (var c in worldCampaigns)
        {
            c.IsActive = false;
        }

        // Activate this campaign
        campaign.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Activated campaign {CampaignId} in world {WorldId}", campaignId, campaign.WorldId);

        return true;
    }

    public async Task<ActiveContextDto> GetActiveContextAsync(Guid worldId, Guid userId)
    {
        var result = new ActiveContextDto();
        result.WorldId = worldId;

        // Check if user has access to this world
        var hasAccess = await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == userId);

        if (!hasAccess)
            return result;

        // Get all campaigns in this world
        var campaigns = await _context.Campaigns
            .Where(c => c.WorldId == worldId)
            .ToListAsync();

        if (campaigns.Count == 0)
            return result;

        // Find explicitly active campaign only
        Campaign? activeCampaign = campaigns.FirstOrDefault(c => c.IsActive);

        if (activeCampaign == null)
            return result;

        result.CampaignId = activeCampaign.Id;
        result.CampaignName = activeCampaign.Name;

        // Get arcs for the active campaign
        var arcs = await _context.Arcs
            .Where(a => a.CampaignId == activeCampaign.Id)
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        if (arcs.Count == 0)
            return result;

        // Find explicitly active arc only
        Arc? activeArc = arcs.FirstOrDefault(a => a.IsActive);

        if (activeArc != null)
        {
            result.ArcId = activeArc.Id;
            result.ArcName = activeArc.Name;
        }

        return result;
    }

    private static CampaignDto MapToDto(Campaign campaign)
    {
        return new CampaignDto
        {
            Id = campaign.Id,
            WorldId = campaign.WorldId,
            Name = campaign.Name,
            Description = campaign.Description,
            OwnerId = campaign.OwnerId,
            OwnerName = campaign.Owner?.DisplayName ?? "Unknown",
            CreatedAt = campaign.CreatedAt,
            StartedAt = campaign.StartedAt,
            EndedAt = campaign.EndedAt,
            IsActive = campaign.IsActive,
            ArcCount = campaign.Arcs?.Count ?? 0
        };
    }

    private static CampaignDetailDto MapToDetailDto(Campaign campaign)
    {
        return new CampaignDetailDto
        {
            Id = campaign.Id,
            WorldId = campaign.WorldId,
            Name = campaign.Name,
            Description = campaign.Description,
            OwnerId = campaign.OwnerId,
            OwnerName = campaign.Owner?.DisplayName ?? "Unknown",
            CreatedAt = campaign.CreatedAt,
            StartedAt = campaign.StartedAt,
            EndedAt = campaign.EndedAt,
            IsActive = campaign.IsActive,
            ArcCount = campaign.Arcs?.Count ?? 0,
            Arcs = campaign.Arcs?.Select(a => new ArcDto
            {
                Id = a.Id,
                CampaignId = a.CampaignId,
                Name = a.Name,
                Description = a.Description,
                SortOrder = a.SortOrder,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt
            }).ToList() ?? new List<ArcDto>()
        };
    }
}
