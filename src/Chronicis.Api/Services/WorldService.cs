using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
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

    // Regex for valid public slug: lowercase alphanumeric with hyphens, no leading/trailing hyphens
    private static readonly Regex PublicSlugRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

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

        var now = DateTime.UtcNow;

        // Generate unique slug for this owner
        var slug = await GenerateUniqueWorldSlugAsync(dto.Name, userId);

        // Create the World entity
        var world = new World
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            OwnerId = userId,
            CreatedAt = now,
            IsPublic = false,
            PublicSlug = null
        };
        _context.Worlds.Add(world);

        // Create default Wiki articles
        var wikiArticles = new[]
        {
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Bestiary",
                Slug = "bestiary",
                Body = "# Bestiary\n\nA collection of creatures and monsters encountered in your adventures.",
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                WorldId = world.Id,
                CreatedBy = userId,
                CreatedAt = now,
                EffectiveDate = now,
                IconEmoji = "🐉"
            },
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Characters",
                Slug = "characters",
                Body = "# Characters\n\nNPCs and notable figures in your world.",
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                WorldId = world.Id,
                CreatedBy = userId,
                CreatedAt = now,
                EffectiveDate = now,
                IconEmoji = "👤"
            },
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Factions",
                Slug = "factions",
                Body = "# Factions\n\nOrganizations, guilds, and groups that shape your world.",
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                WorldId = world.Id,
                CreatedBy = userId,
                CreatedAt = now,
                EffectiveDate = now,
                IconEmoji = "⚔️"
            },
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Locations",
                Slug = "locations",
                Body = "# Locations\n\nPlaces of interest, cities, dungeons, and landmarks.",
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                WorldId = world.Id,
                CreatedBy = userId,
                CreatedAt = now,
                EffectiveDate = now,
                IconEmoji = "🗺️"
            }
        };
        _context.Articles.AddRange(wikiArticles);

        // Create default Player Character
        var newCharacter = new Article
        {
            Id = Guid.NewGuid(),
            Title = "New Character",
            Slug = "new-character",
            Body = "# New Character\n\nDescribe your character here. Add their backstory, personality, and goals.",
            Type = ArticleType.Character,
            Visibility = ArticleVisibility.Public,
            WorldId = world.Id,
            CreatedBy = userId,
            PlayerId = userId,
            CreatedAt = now,
            EffectiveDate = now,
            IconEmoji = "🧙"
        };
        _context.Articles.Add(newCharacter);

        // Create default Campaign
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Campaign 1",
            Description = "Your first campaign adventure begins here.",
            WorldId = world.Id,
            OwnerId = userId,
            CreatedAt = now
        };
        _context.Campaigns.Add(campaign);

        // Create default Arc under the campaign
        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            Name = "Arc 1",
            Description = "The first chapter of your adventure.",
            CampaignId = campaign.Id,
            SortOrder = 1,
            CreatedBy = userId,
            CreatedAt = now
        };
        _context.Arcs.Add(arc);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created world {WorldId} with default content for user {UserId}", world.Id, userId);

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

        // If name changed, regenerate slug
        if (world.Name != dto.Name)
        {
            world.Slug = await GenerateUniqueWorldSlugAsync(dto.Name, userId, world.Id);
        }

        world.Name = dto.Name;
        world.Description = dto.Description;

        // Handle public visibility changes if specified
        if (dto.IsPublic.HasValue)
        {
            if (dto.IsPublic.Value)
            {
                // Making world public - require a valid public slug
                if (string.IsNullOrWhiteSpace(dto.PublicSlug))
                {
                    _logger.LogWarning("Attempted to make world {WorldId} public without a public slug", worldId);
                    return null; // Or throw a validation exception
                }

                var normalizedSlug = dto.PublicSlug.Trim().ToLowerInvariant();
                
                // Validate slug format
                var validationError = ValidatePublicSlug(normalizedSlug);
                if (validationError != null)
                {
                    _logger.LogWarning("Invalid public slug '{Slug}' for world {WorldId}: {Error}", 
                        normalizedSlug, worldId, validationError);
                    return null;
                }

                // Check availability
                if (!await IsPublicSlugAvailableAsync(normalizedSlug, worldId))
                {
                    _logger.LogWarning("Public slug '{Slug}' is already taken", normalizedSlug);
                    return null;
                }

                world.IsPublic = true;
                world.PublicSlug = normalizedSlug;
                
                _logger.LogInformation("World {WorldId} is now public with slug '{PublicSlug}'", worldId, normalizedSlug);
            }
            else
            {
                // Making world private - clear public slug
                world.IsPublic = false;
                world.PublicSlug = null;
                
                _logger.LogInformation("World {WorldId} is now private", worldId);
            }
        }

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

    /// <summary>
    /// Check if a public slug is available (not already in use by another world).
    /// </summary>
    public async Task<bool> IsPublicSlugAvailableAsync(string publicSlug, Guid? excludeWorldId = null)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var query = _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug);

        if (excludeWorldId.HasValue)
        {
            query = query.Where(w => w.Id != excludeWorldId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Check public slug availability and return detailed result.
    /// </summary>
    public async Task<PublicSlugCheckResultDto> CheckPublicSlugAsync(string slug, Guid? excludeWorldId = null)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        // Validate format first
        var validationError = ValidatePublicSlug(normalizedSlug);
        if (validationError != null)
        {
            return new PublicSlugCheckResultDto
            {
                IsAvailable = false,
                ValidationError = validationError,
                SuggestedSlug = GenerateSuggestedSlug(slug)
            };
        }

        // Check availability
        var isAvailable = await IsPublicSlugAvailableAsync(normalizedSlug, excludeWorldId);
        
        return new PublicSlugCheckResultDto
        {
            IsAvailable = isAvailable,
            ValidationError = null,
            SuggestedSlug = isAvailable ? null : await GenerateAvailableSlugAsync(normalizedSlug)
        };
    }

    /// <summary>
    /// Get a world by its public slug (for anonymous access).
    /// </summary>
    public async Task<WorldDto?> GetWorldByPublicSlugAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync(w => w.PublicSlug == normalizedSlug && w.IsPublic);

        if (world == null)
            return null;

        return MapToDto(world);
    }

    /// <summary>
    /// Validate a public slug format.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    private static string? ValidatePublicSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Public slug is required";

        if (slug.Length < 3)
            return "Public slug must be at least 3 characters";

        if (slug.Length > 100)
            return "Public slug must be 100 characters or less";

        if (!PublicSlugRegex.IsMatch(slug))
            return "Public slug must contain only lowercase letters, numbers, and hyphens (no leading/trailing hyphens)";

        // Reserved slugs that shouldn't be used
        var reserved = new[] { "api", "admin", "public", "private", "new", "edit", "delete", "search", "login", "logout", "settings" };
        if (reserved.Contains(slug))
            return "This slug is reserved and cannot be used";

        return null;
    }

    /// <summary>
    /// Generate a suggested slug from user input.
    /// </summary>
    private static string GenerateSuggestedSlug(string input)
    {
        // Convert to lowercase, replace spaces and underscores with hyphens
        var slug = input.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        // Remove consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");
        // Remove leading/trailing hyphens
        slug = slug.Trim('-');

        // Ensure minimum length
        if (slug.Length < 3)
            slug = slug.PadRight(3, '0');

        return slug;
    }

    /// <summary>
    /// Generate an available slug by appending numbers.
    /// </summary>
    private async Task<string> GenerateAvailableSlugAsync(string baseSlug)
    {
        var suffix = 1;
        var candidate = $"{baseSlug}-{suffix}";

        while (!await IsPublicSlugAvailableAsync(candidate))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";

            // Safety limit
            if (suffix > 100)
                return $"{baseSlug}-{Guid.NewGuid().ToString()[..8]}";
        }

        return candidate;
    }

    /// <summary>
    /// Generate a unique slug for a world within an owner's worlds.
    /// </summary>
    private async Task<string> GenerateUniqueWorldSlugAsync(string name, Guid ownerId, Guid? excludeWorldId = null)
    {
        var baseSlug = SlugGenerator.GenerateSlug(name);

        var existingSlugsQuery = _context.Worlds
            .AsNoTracking()
            .Where(w => w.OwnerId == ownerId);

        if (excludeWorldId.HasValue)
        {
            existingSlugsQuery = existingSlugsQuery.Where(w => w.Id != excludeWorldId.Value);
        }

        var existingSlugs = await existingSlugsQuery
            .Select(w => w.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
    }

    /// <summary>
    /// Get a world by its slug for a specific owner.
    /// </summary>
    public async Task<WorldDto?> GetWorldBySlugAsync(string slug, Guid userId)
    {
        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync(w => w.Slug == slug && w.OwnerId == userId);

        if (world == null)
            return null;

        return MapToDto(world);
    }

    private static WorldDto MapToDto(World world)
    {
        return new WorldDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            IsPublic = world.IsPublic,
            PublicSlug = world.PublicSlug
        };
    }

    private static WorldDetailDto MapToDetailDto(World world)
    {
        return new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            IsPublic = world.IsPublic,
            PublicSlug = world.PublicSlug,
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
