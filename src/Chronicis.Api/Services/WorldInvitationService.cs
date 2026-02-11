using Chronicis.Api.Data;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for world invitation management
/// </summary>
public class WorldInvitationService : IWorldInvitationService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<WorldInvitationService> _logger;

    public WorldInvitationService(ChronicisDbContext context, ILogger<WorldInvitationService> logger)
    {
        _context = context;
        _logger = logger;
    }

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

        _logger.LogDebugSanitized("Created invitation {Code} for world {WorldId} by user {UserId}", 
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

        _logger.LogDebug("Revoked invitation {InvitationId} for world {WorldId}", invitationId, worldId);

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

        _logger.LogDebugSanitized("User {UserId} joined world {WorldId} via invitation {Code}", 
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
