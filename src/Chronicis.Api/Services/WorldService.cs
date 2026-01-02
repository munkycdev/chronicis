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
        // Get all worlds user is a member of (via WorldMembers)
        var worlds = await _context.Worlds
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .Include(w => w.Members)
            .ToListAsync();

        return worlds.Select(MapToDto).ToList();
    }

    public async Task<WorldDetailDto?> GetWorldAsync(Guid worldId, Guid userId)
    {
        var world = await _context.Worlds
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Owner)
            .Include(w => w.Members)
                .ThenInclude(m => m.User)
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
        // User is a member of this world
        return await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == userId);
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
    /// <summary>
    /// Get a world by its slug - user must be a member of the world.
    /// </summary>
    public async Task<WorldDto?> GetWorldBySlugAsync(string slug, Guid userId)
    {
        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Slug == slug && w.Members.Any(m => m.UserId == userId));

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
            MemberCount = world.Members?.Count ?? 0,
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
            MemberCount = world.Members?.Count ?? 0,
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
                EndedAt = c.EndedAt
            }).ToList() ?? new List<CampaignDto>(),
            Members = world.Members?.Select(m => new WorldMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                DisplayName = m.User?.DisplayName ?? "Unknown",
                Email = m.User?.Email ?? "",
                AvatarUrl = m.User?.AvatarUrl,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                InvitedBy = m.InvitedBy
            }).ToList() ?? new List<WorldMemberDto>()
        };
    }

    // ===== Member Management =====

    public async Task<List<WorldMemberDto>> GetMembersAsync(Guid worldId, Guid userId)
    {
        // Check access
        if (!await UserHasAccessAsync(worldId, userId))
            return new List<WorldMemberDto>();

        var members = await _context.WorldMembers
            .Include(m => m.User)
            .Include(m => m.Inviter)
            .Where(m => m.WorldId == worldId)
            .ToListAsync();

        return members.Select(m => new WorldMemberDto
        {
            Id = m.Id,
            UserId = m.UserId,
            DisplayName = m.User?.DisplayName ?? "Unknown",
            Email = m.User?.Email ?? "",
            AvatarUrl = m.User?.AvatarUrl,
            Role = m.Role,
            JoinedAt = m.JoinedAt,
            InvitedBy = m.InvitedBy,
            InviterName = m.Inviter?.DisplayName
        }).ToList();
    }

    public async Task<WorldMemberDto?> UpdateMemberRoleAsync(Guid worldId, Guid memberId, WorldMemberUpdateDto dto, Guid userId)
    {
        // Only GMs can update roles
        var isGM = await _context.WorldMembers
            .AnyAsync(m => m.WorldId == worldId && m.UserId == userId && m.Role == WorldRole.GM);

        if (!isGM)
            return null;

        var member = await _context.WorldMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.WorldId == worldId);

        if (member == null)
            return null;

        // Prevent demoting the last GM
        if (member.Role == WorldRole.GM && dto.Role != WorldRole.GM)
        {
            var gmCount = await _context.WorldMembers
                .CountAsync(m => m.WorldId == worldId && m.Role == WorldRole.GM);

            if (gmCount <= 1)
            {
                _logger.LogWarning("Cannot demote the last GM of world {WorldId}", worldId);
                return null;
            }
        }

        member.Role = dto.Role;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated member {MemberId} role to {Role} in world {WorldId}", 
            memberId, dto.Role, worldId);

        return new WorldMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = member.User?.DisplayName ?? "Unknown",
            Email = member.User?.Email ?? "",
            AvatarUrl = member.User?.AvatarUrl,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            InvitedBy = member.InvitedBy
        };
    }

    public async Task<bool> RemoveMemberAsync(Guid worldId, Guid memberId, Guid userId)
    {
        // Only GMs can remove members
        var isGM = await _context.WorldMembers
            .AnyAsync(m => m.WorldId == worldId && m.UserId == userId && m.Role == WorldRole.GM);

        if (!isGM)
            return false;

        var member = await _context.WorldMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.WorldId == worldId);

        if (member == null)
            return false;

        // Prevent removing the last GM
        if (member.Role == WorldRole.GM)
        {
            var gmCount = await _context.WorldMembers
                .CountAsync(m => m.WorldId == worldId && m.Role == WorldRole.GM);

            if (gmCount <= 1)
            {
                _logger.LogWarning("Cannot remove the last GM of world {WorldId}", worldId);
                return false;
            }
        }

        _context.WorldMembers.Remove(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed member {MemberId} from world {WorldId}", memberId, worldId);

        return true;
    }

    // ===== Invitation Management =====

    public async Task<List<WorldInvitationDto>> GetInvitationsAsync(Guid worldId, Guid userId)
    {
        // Only GMs can view invitations
        var isGM = await _context.WorldMembers
            .AnyAsync(m => m.WorldId == worldId && m.UserId == userId && m.Role == WorldRole.GM);

        if (!isGM)
            return new List<WorldInvitationDto>();

        var invitations = await _context.WorldInvitations
            .Include(i => i.Creator)
            .Where(i => i.WorldId == worldId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invitations.Select(i => new WorldInvitationDto
        {
            Id = i.Id,
            WorldId = i.WorldId,
            Code = i.Code,
            Role = i.Role,
            CreatedBy = i.CreatedBy,
            CreatorName = i.Creator?.DisplayName ?? "Unknown",
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt,
            MaxUses = i.MaxUses,
            UsedCount = i.UsedCount,
            IsActive = i.IsActive
        }).ToList();
    }

    public async Task<WorldInvitationDto?> CreateInvitationAsync(Guid worldId, WorldInvitationCreateDto dto, Guid userId)
    {
        // Only GMs can create invitations
        var isGM = await _context.WorldMembers
            .AnyAsync(m => m.WorldId == worldId && m.UserId == userId && m.Role == WorldRole.GM);

        if (!isGM)
            return null;

        // Generate unique code
        string code;
        int attempts = 0;
        do
        {
            code = Utilities.InvitationCodeGenerator.GenerateCode();
            attempts++;
        } while (await _context.WorldInvitations.AnyAsync(i => i.Code == code) && attempts < 10);

        if (attempts >= 10)
        {
            _logger.LogError("Failed to generate unique invitation code after 10 attempts");
            return null;
        }

        var invitation = new WorldInvitation
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Code = code,
            Role = dto.Role,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            MaxUses = dto.MaxUses,
            UsedCount = 0,
            IsActive = true
        };

        _context.WorldInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created invitation {Code} for world {WorldId} by user {UserId}", 
            code, worldId, userId);

        var creator = await _context.Users.FindAsync(userId);

        return new WorldInvitationDto
        {
            Id = invitation.Id,
            WorldId = invitation.WorldId,
            Code = invitation.Code,
            Role = invitation.Role,
            CreatedBy = invitation.CreatedBy,
            CreatorName = creator?.DisplayName ?? "Unknown",
            CreatedAt = invitation.CreatedAt,
            ExpiresAt = invitation.ExpiresAt,
            MaxUses = invitation.MaxUses,
            UsedCount = invitation.UsedCount,
            IsActive = invitation.IsActive
        };
    }

    public async Task<bool> RevokeInvitationAsync(Guid worldId, Guid invitationId, Guid userId)
    {
        // Only GMs can revoke invitations
        var isGM = await _context.WorldMembers
            .AnyAsync(m => m.WorldId == worldId && m.UserId == userId && m.Role == WorldRole.GM);

        if (!isGM)
            return false;

        var invitation = await _context.WorldInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.WorldId == worldId);

        if (invitation == null)
            return false;

        invitation.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked invitation {InvitationId} for world {WorldId}", invitationId, worldId);

        return true;
    }

    public async Task<WorldJoinResultDto> JoinWorldAsync(string code, Guid userId)
    {
        var normalizedCode = Utilities.InvitationCodeGenerator.NormalizeCode(code);

        if (!Utilities.InvitationCodeGenerator.IsValidFormat(normalizedCode))
        {
            return new WorldJoinResultDto
            {
                Success = false,
                ErrorMessage = "Invalid invitation code format"
            };
        }

        var invitation = await _context.WorldInvitations
            .Include(i => i.World)
            .FirstOrDefaultAsync(i => i.Code == normalizedCode && i.IsActive);

        if (invitation == null)
        {
            return new WorldJoinResultDto
            {
                Success = false,
                ErrorMessage = "Invitation not found or has been revoked"
            };
        }

        // Check expiration
        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new WorldJoinResultDto
            {
                Success = false,
                ErrorMessage = "This invitation has expired"
            };
        }

        // Check max uses
        if (invitation.MaxUses.HasValue && invitation.UsedCount >= invitation.MaxUses.Value)
        {
            return new WorldJoinResultDto
            {
                Success = false,
                ErrorMessage = "This invitation has reached its maximum number of uses"
            };
        }

        // Check if user is already a member
        var existingMember = await _context.WorldMembers
            .FirstOrDefaultAsync(m => m.WorldId == invitation.WorldId && m.UserId == userId);

        if (existingMember != null)
        {
            return new WorldJoinResultDto
            {
                Success = false,
                ErrorMessage = "You are already a member of this world"
            };
        }

        // Create membership
        var member = new WorldMember
        {
            Id = Guid.NewGuid(),
            WorldId = invitation.WorldId,
            UserId = userId,
            Role = invitation.Role,
            JoinedAt = DateTime.UtcNow,
            InvitedBy = invitation.CreatedBy
        };

        _context.WorldMembers.Add(member);

        // Increment usage count
        invitation.UsedCount++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} joined world {WorldId} via invitation {Code}", 
            userId, invitation.WorldId, normalizedCode);

        return new WorldJoinResultDto
        {
            Success = true,
            WorldId = invitation.WorldId,
            WorldName = invitation.World?.Name,
            AssignedRole = invitation.Role
        };
    }
}
