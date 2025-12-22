using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return null;

        // Check access
        if (!await UserHasAccessAsync(campaignId, userId))
            return null;

        return MapToDetailDto(campaign);
    }

    public async Task<CampaignDto> CreateCampaignAsync(CampaignCreateDto dto, Guid userId)
    {
        // Verify user owns the world
        var world = await _context.Worlds.FindAsync(dto.WorldId);
        if (world == null)
            throw new InvalidOperationException("World not found");

        if (world.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the world owner can create campaigns");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        _logger.LogInformation("Creating campaign '{Name}' in world {WorldId} for user {UserId}", 
            dto.Name, dto.WorldId, userId);

        // Create the Campaign
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

        // Add creator as DM
        var dmMember = new CampaignMember
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            UserId = userId,
            Role = CampaignRole.DM,
            JoinedAt = DateTime.UtcNow
        };
        _context.CampaignMembers.Add(dmMember);

        // Find the CampaignRoot article for this world
        var campaignRoot = await _context.Articles
            .FirstOrDefaultAsync(a => a.WorldId == dto.WorldId && a.Type == ArticleType.CampaignRoot);

        if (campaignRoot == null)
            throw new InvalidOperationException("Campaign root not found for world");

        // Create Campaign article under CampaignRoot
        var campaignSlug = SlugGenerator.GenerateSlug(dto.Name);
        var campaignArticle = new Article
        {
            Id = Guid.NewGuid(),
            Type = ArticleType.Campaign,
            Title = dto.Name,
            Slug = campaignSlug,
            Body = dto.Description ?? string.Empty,
            WorldId = dto.WorldId,
            CampaignId = campaign.Id,
            ParentId = campaignRoot.Id,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
            Visibility = ArticleVisibility.Public
        };
        _context.Articles.Add(campaignArticle);

        // Create Act 1 under Campaign article
        var act1Article = new Article
        {
            Id = Guid.NewGuid(),
            Type = ArticleType.Act,
            Title = "Act 1",
            Slug = "act-1",
            Body = string.Empty,
            WorldId = dto.WorldId,
            CampaignId = campaign.Id,
            ParentId = campaignArticle.Id,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
            Visibility = ArticleVisibility.Public
        };
        _context.Articles.Add(act1Article);

        // Create SharedInfoRoot under Act 1
        var sharedInfoRoot = new Article
        {
            Id = Guid.NewGuid(),
            Type = ArticleType.SharedInfoRoot,
            Title = "Shared Information",
            Slug = "shared-information",
            Body = string.Empty,
            WorldId = dto.WorldId,
            CampaignId = campaign.Id,
            ParentId = act1Article.Id,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
            Visibility = ArticleVisibility.Public
        };
        _context.Articles.Add(sharedInfoRoot);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created campaign {CampaignId} with Act 1 and Shared Information for user {UserId}", 
            campaign.Id, userId);

        // Return DTO
        campaign.Owner = user;
        campaign.Members = new List<CampaignMember> { dmMember };
        return MapToDto(campaign);
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(Guid campaignId, CampaignUpdateDto dto, Guid userId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Owner)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return null;

        // Only DM can update
        if (!await UserIsDungeonMasterAsync(campaignId, userId))
            return null;

        campaign.Name = dto.Name;
        campaign.Description = dto.Description;
        campaign.StartedAt = dto.StartedAt;
        campaign.EndedAt = dto.EndedAt;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated campaign {CampaignId}", campaignId);

        return MapToDto(campaign);
    }

    public async Task<CampaignMemberDto?> AddMemberAsync(Guid campaignId, CampaignMemberAddDto dto, Guid requestingUserId)
    {
        // Only DM can add members
        if (!await UserIsDungeonMasterAsync(campaignId, requestingUserId))
            return null;

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            _logger.LogWarning("Cannot add member: user with email {Email} not found", dto.Email);
            return null;
        }

        // Check if already a member
        var existingMember = await _context.CampaignMembers
            .FirstOrDefaultAsync(cm => cm.CampaignId == campaignId && cm.UserId == user.Id);

        if (existingMember != null)
        {
            _logger.LogWarning("User {UserId} is already a member of campaign {CampaignId}", user.Id, campaignId);
            return null;
        }

        var member = new CampaignMember
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            UserId = user.Id,
            Role = dto.Role,
            JoinedAt = DateTime.UtcNow,
            CharacterName = dto.CharacterName
        };
        _context.CampaignMembers.Add(member);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added user {UserId} to campaign {CampaignId} with role {Role}", 
            user.Id, campaignId, dto.Role);

        member.User = user;
        return MapMemberToDto(member);
    }

    public async Task<CampaignMemberDto?> UpdateMemberAsync(Guid campaignId, Guid userId, CampaignMemberUpdateDto dto, Guid requestingUserId)
    {
        // Only DM can update members
        if (!await UserIsDungeonMasterAsync(campaignId, requestingUserId))
            return null;

        var member = await _context.CampaignMembers
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.CampaignId == campaignId && cm.UserId == userId);

        if (member == null)
            return null;

        // Prevent demoting the last DM
        if (member.Role == CampaignRole.DM && dto.Role != CampaignRole.DM)
        {
            var dmCount = await _context.CampaignMembers
                .CountAsync(cm => cm.CampaignId == campaignId && cm.Role == CampaignRole.DM);

            if (dmCount <= 1)
            {
                _logger.LogWarning("Cannot demote the last DM of campaign {CampaignId}", campaignId);
                return null;
            }
        }

        member.Role = dto.Role;
        member.CharacterName = dto.CharacterName;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated member {UserId} in campaign {CampaignId} to role {Role}", 
            userId, campaignId, dto.Role);

        return MapMemberToDto(member);
    }

    public async Task<bool> RemoveMemberAsync(Guid campaignId, Guid userId, Guid requestingUserId)
    {
        // DM can remove anyone except themselves if they're the last DM
        // Users can remove themselves (leave)
        var isDm = await UserIsDungeonMasterAsync(campaignId, requestingUserId);
        var isSelf = requestingUserId == userId;

        if (!isDm && !isSelf)
            return false;

        var member = await _context.CampaignMembers
            .FirstOrDefaultAsync(cm => cm.CampaignId == campaignId && cm.UserId == userId);

        if (member == null)
            return false;

        // Prevent removing the last DM
        if (member.Role == CampaignRole.DM)
        {
            var dmCount = await _context.CampaignMembers
                .CountAsync(cm => cm.CampaignId == campaignId && cm.Role == CampaignRole.DM);

            if (dmCount <= 1)
            {
                _logger.LogWarning("Cannot remove the last DM of campaign {CampaignId}", campaignId);
                return false;
            }
        }

        _context.CampaignMembers.Remove(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed user {UserId} from campaign {CampaignId}", userId, campaignId);

        return true;
    }

    public async Task<CampaignRole?> GetUserRoleAsync(Guid campaignId, Guid userId)
    {
        var member = await _context.CampaignMembers
            .FirstOrDefaultAsync(cm => cm.CampaignId == campaignId && cm.UserId == userId);

        return member?.Role;
    }

    public async Task<bool> UserHasAccessAsync(Guid campaignId, Guid userId)
    {
        return await _context.CampaignMembers
            .AnyAsync(cm => cm.CampaignId == campaignId && cm.UserId == userId);
    }

    public async Task<bool> UserIsDungeonMasterAsync(Guid campaignId, Guid userId)
    {
        return await _context.CampaignMembers
            .AnyAsync(cm => cm.CampaignId == campaignId 
                        && cm.UserId == userId 
                        && cm.Role == CampaignRole.DM);
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
            MemberCount = campaign.Members?.Count ?? 0
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
            MemberCount = campaign.Members?.Count ?? 0,
            Members = campaign.Members?.Select(MapMemberToDto).ToList() ?? new List<CampaignMemberDto>()
        };
    }

    private static CampaignMemberDto MapMemberToDto(CampaignMember member)
    {
        return new CampaignMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = member.User?.DisplayName ?? "Unknown",
            Email = member.User?.Email ?? "",
            AvatarUrl = member.User?.AvatarUrl,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            CharacterName = member.CharacterName
        };
    }
}
