using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for world management
/// </summary>
public class WorldService : IWorldService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<WorldService> _logger;

    public WorldService(ChronicisDbContext context, ILogger<WorldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WorldDto>> GetUserWorldsAsync(Guid userId)
    {
        // Get worlds user owns
        var ownedWorlds = await _context.Worlds
            .Where(w => w.OwnerId == userId)
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .ToListAsync();

        // Get worlds user is a member of (via campaign membership)
        var memberWorldIds = await _context.CampaignMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.Campaign.WorldId)
            .Distinct()
            .ToListAsync();

        var memberWorlds = await _context.Worlds
            .Where(w => memberWorldIds.Contains(w.Id) && w.OwnerId != userId)
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .ToListAsync();

        var allWorlds = ownedWorlds.Concat(memberWorlds).ToList();

        return allWorlds.Select(MapToDto).ToList();
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId, Guid userId)
    {
        var world = await _context.Worlds
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Owner)
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Members)
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
            return null;

        // Check access
        if (!await UserHasAccessAsync(worldId, userId))
            return null;

        return MapToDetailDto(world);
    }

    public async Task<WorldDto> CreateWorldAsync(WorldCreateDto dto, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        _logger.LogInformation("Creating world '{Name}' for user {UserId}", dto.Name, userId);

        // Create the World
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Worlds.Add(world);

        // Create root structure articles
        var worldRoot = CreateRootArticle(ArticleType.WorldRoot, "World", "world", world.Id, null, userId);
        _context.Articles.Add(worldRoot);

        var wikiRoot = CreateRootArticle(ArticleType.WikiRoot, "Wiki", "wiki", world.Id, worldRoot.Id, userId);
        _context.Articles.Add(wikiRoot);

        var campaignRoot = CreateRootArticle(ArticleType.CampaignRoot, "Campaigns", "campaigns", world.Id, worldRoot.Id, userId);
        _context.Articles.Add(campaignRoot);

        var characterRoot = CreateRootArticle(ArticleType.CharacterRoot, "Characters", "characters", world.Id, worldRoot.Id, userId);
        _context.Articles.Add(characterRoot);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created world {WorldId} with root structure for user {UserId}", world.Id, userId);

        // Return DTO
        world.Owner = user;
        return MapToDto(world);
    }

    public async Task<WorldDto?> UpdateWorldAsync(Guid worldId, WorldUpdateDto dto, Guid userId)
    {
        var world = await _context.Worlds
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
            return null;

        // Only owner can update
        if (world.OwnerId != userId)
            return null;

        world.Name = dto.Name;
        world.Description = dto.Description;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated world {WorldId}", worldId);

        return MapToDto(world);
    }

    public async Task<bool> UserHasAccessAsync(Guid worldId, Guid userId)
    {
        // User owns the world
        var ownsWorld = await _context.Worlds
            .AnyAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (ownsWorld)
            return true;

        // User is a member of a campaign in this world
        var isMember = await _context.CampaignMembers
            .AnyAsync(cm => cm.UserId == userId && cm.Campaign.WorldId == worldId);

        return isMember;
    }

    public async Task<bool> UserOwnsWorldAsync(Guid worldId, Guid userId)
    {
        return await _context.Worlds
            .AnyAsync(w => w.Id == worldId && w.OwnerId == userId);
    }

    private static Article CreateRootArticle(ArticleType type, string title, string slug, Guid worldId, Guid? parentId, Guid userId)
    {
        return new Article
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Slug = slug,
            WorldId = worldId,
            ParentId = parentId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = DateTime.UtcNow,
            Visibility = ArticleVisibility.Public,
            Body = string.Empty
        };
    }

    private static WorldDto MapToDto(World world)
    {
        return new WorldDto
        {
            Id = world.Id,
            Name = world.Name,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0
        };
    }

    private static WorldDetailDto MapToDetailDto(World world)
    {
        return new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            Campaigns = world.Campaigns?.Select(c => new CampaignDto
            {
                Id = c.Id,
                WorldId = c.WorldId,
                Name = c.Name,
                Description = c.Description,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner?.DisplayName ?? "Unknown",
                CreatedAt = c.CreatedAt,
                StartedAt = c.StartedAt,
                EndedAt = c.EndedAt,
                MemberCount = c.Members?.Count ?? 0
            }).ToList() ?? new List<CampaignDto>()
        };
    }
}
