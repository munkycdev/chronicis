using Chronicis.Api.Data;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for world membership and access control
/// </summary>
public class WorldMembershipService : IWorldMembershipService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<WorldMembershipService> _logger;

    public WorldMembershipService(ChronicisDbContext context, ILogger<WorldMembershipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> UserHasAccessAsync(Guid worldId, Guid userId)
    {
        return await _context.WorldMembers
            .AnyAsync(wm => wm.WorldId == worldId && wm.UserId == userId);
    }

    public async Task<bool> UserOwnsWorldAsync(Guid worldId, Guid userId)
    {
        return await _context.Worlds
            .AnyAsync(w => w.Id == worldId && w.OwnerId == userId);
    }

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

        _logger.LogDebug("Updated member {MemberId} role to {Role} in world {WorldId}", 
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

        _logger.LogDebug("Removed member {MemberId} from world {WorldId}", memberId, worldId);

        return true;
    }
}
