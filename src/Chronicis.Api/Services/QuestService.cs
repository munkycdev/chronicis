using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for Quest operations with world membership authorization.
/// </summary>
public class QuestService : IQuestService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<QuestService> _logger;

    public QuestService(ChronicisDbContext context, ILogger<QuestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<List<QuestDto>>> GetQuestsByArcAsync(Guid arcId, Guid userId)
    {
        // Check if user has access to this arc via world membership
        var arc = await _context.Arcs
            .AsNoTracking()
            .Include(a => a.Campaign)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            return ServiceResult<List<QuestDto>>.NotFound("Arc not found");
        }

        // Check world membership
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            // Don't disclose arc existence to non-members
            return ServiceResult<List<QuestDto>>.NotFound("Arc not found");
        }

        var isGM = member.Role == WorldRole.GM;

        // Build query - filter IsGmOnly for non-GM users
        var query = _context.Quests
            .AsNoTracking()
            .Where(q => q.ArcId == arcId);

        if (!isGM)
        {
            query = query.Where(q => !q.IsGmOnly);
        }

        var quests = await query
            .OrderBy(q => q.SortOrder)
            .ThenByDescending(q => q.UpdatedAt)
            .Select(q => new QuestDto
            {
                Id = q.Id,
                ArcId = q.ArcId,
                Title = q.Title,
                Description = q.Description,
                Status = q.Status,
                IsGmOnly = q.IsGmOnly,
                SortOrder = q.SortOrder,
                CreatedBy = q.CreatedBy,
                CreatedByName = q.Creator.DisplayName,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                RowVersion = Convert.ToBase64String(q.RowVersion),
                UpdateCount = q.Updates.Count
            })
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} quests for arc {ArcId} for user {UserId} (GM: {IsGM})",
            quests.Count, arcId, userId, isGM);

        return ServiceResult<List<QuestDto>>.Success(quests);
    }

    public async Task<ServiceResult<QuestDto>> GetQuestAsync(Guid questId, Guid userId)
    {
        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Arc)
                .ThenInclude(a => a.Campaign)
            .Include(q => q.Creator)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
        {
            return ServiceResult<QuestDto>.NotFound("Quest not found");
        }

        // Check world membership
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            // Don't disclose quest existence to non-members
            return ServiceResult<QuestDto>.NotFound("Quest not found");
        }

        var isGM = member.Role == WorldRole.GM;

        // Non-GM users cannot see GM-only quests
        if (quest.IsGmOnly && !isGM)
        {
            return ServiceResult<QuestDto>.NotFound("Quest not found");
        }

        var updateCount = await _context.QuestUpdates
            .CountAsync(qu => qu.QuestId == questId);

        var dto = new QuestDto
        {
            Id = quest.Id,
            ArcId = quest.ArcId,
            Title = quest.Title,
            Description = quest.Description,
            Status = quest.Status,
            IsGmOnly = quest.IsGmOnly,
            SortOrder = quest.SortOrder,
            CreatedBy = quest.CreatedBy,
            CreatedByName = quest.Creator.DisplayName,
            CreatedAt = quest.CreatedAt,
            UpdatedAt = quest.UpdatedAt,
            RowVersion = Convert.ToBase64String(quest.RowVersion),
            UpdateCount = updateCount
        };

        return ServiceResult<QuestDto>.Success(dto);
    }

    public async Task<ServiceResult<QuestDto>> CreateQuestAsync(Guid arcId, QuestCreateDto dto, Guid userId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return ServiceResult<QuestDto>.ValidationError("Title is required");
        }

        if (dto.Title.Length > 300)
        {
            return ServiceResult<QuestDto>.ValidationError("Title cannot exceed 300 characters");
        }

        // Check if user has access to this arc and is GM
        var arc = await _context.Arcs
            .AsNoTracking()
            .Include(a => a.Campaign)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            return ServiceResult<QuestDto>.NotFound("Arc not found");
        }

        // Check world membership and GM role
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<QuestDto>.NotFound("Arc not found");
        }

        if (member.Role != WorldRole.GM)
        {
            return ServiceResult<QuestDto>.Forbidden("Only GMs can create quests");
        }

        var now = DateTime.UtcNow;
        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            ArcId = arcId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Status = dto.Status ?? QuestStatus.Active,
            IsGmOnly = dto.IsGmOnly ?? false,
            SortOrder = dto.SortOrder ?? 0,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Quests.Add(quest);
        await _context.SaveChangesAsync();

        _logger.LogDebugSanitized("Created quest '{Title}' in arc {ArcId} for user {UserId}",
            dto.Title, arcId, userId);

        // Fetch creator name for DTO
        var creator = await _context.Users.FindAsync(userId);

        var resultDto = new QuestDto
        {
            Id = quest.Id,
            ArcId = quest.ArcId,
            Title = quest.Title,
            Description = quest.Description,
            Status = quest.Status,
            IsGmOnly = quest.IsGmOnly,
            SortOrder = quest.SortOrder,
            CreatedBy = quest.CreatedBy,
            CreatedByName = creator?.DisplayName ?? "Unknown",
            CreatedAt = quest.CreatedAt,
            UpdatedAt = quest.UpdatedAt,
            RowVersion = Convert.ToBase64String(quest.RowVersion),
            UpdateCount = 0
        };

        return ServiceResult<QuestDto>.Success(resultDto);
    }

    public async Task<ServiceResult<QuestDto>> UpdateQuestAsync(Guid questId, QuestEditDto dto, Guid userId)
    {
        // Validate RowVersion is provided
        if (string.IsNullOrWhiteSpace(dto.RowVersion))
        {
            return ServiceResult<QuestDto>.ValidationError("RowVersion is required for updates");
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(dto.RowVersion);
        }
        catch (FormatException)
        {
            return ServiceResult<QuestDto>.ValidationError("Invalid RowVersion format");
        }

        // Validate title if provided
        if (dto.Title != null && string.IsNullOrWhiteSpace(dto.Title))
        {
            return ServiceResult<QuestDto>.ValidationError("Title cannot be empty");
        }

        if (dto.Title != null && dto.Title.Length > 300)
        {
            return ServiceResult<QuestDto>.ValidationError("Title cannot exceed 300 characters");
        }

        // Find quest with tracking for update
        var quest = await _context.Quests
            .Include(q => q.Arc)
                .ThenInclude(a => a.Campaign)
            .Include(q => q.Creator)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
        {
            return ServiceResult<QuestDto>.NotFound("Quest not found");
        }

        // Check world membership and GM role
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<QuestDto>.NotFound("Quest not found");
        }

        if (member.Role != WorldRole.GM)
        {
            return ServiceResult<QuestDto>.Forbidden("Only GMs can update quests");
        }

        // Apply updates
        if (dto.Title != null)
        {
            quest.Title = dto.Title.Trim();
        }

        if (dto.Description != null)
        {
            quest.Description = dto.Description;
        }

        if (dto.Status.HasValue)
        {
            quest.Status = dto.Status.Value;
        }

        if (dto.IsGmOnly.HasValue)
        {
            quest.IsGmOnly = dto.IsGmOnly.Value;
        }

        if (dto.SortOrder.HasValue)
        {
            quest.SortOrder = dto.SortOrder.Value;
        }

        quest.UpdatedAt = DateTime.UtcNow;

        // Set original RowVersion for concurrency check
        _context.Entry(quest).Property(q => q.RowVersion).OriginalValue = rowVersion;

        try
        {
            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated quest {QuestId} for user {UserId}", questId, userId);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Reload current state from database
            await _context.Entry(quest).ReloadAsync();

            var updateCount = await _context.QuestUpdates.CountAsync(qu => qu.QuestId == questId);

            var currentDto = new QuestDto
            {
                Id = quest.Id,
                ArcId = quest.ArcId,
                Title = quest.Title,
                Description = quest.Description,
                Status = quest.Status,
                IsGmOnly = quest.IsGmOnly,
                SortOrder = quest.SortOrder,
                CreatedBy = quest.CreatedBy,
                CreatedByName = quest.Creator.DisplayName,
                CreatedAt = quest.CreatedAt,
                UpdatedAt = quest.UpdatedAt,
                RowVersion = Convert.ToBase64String(quest.RowVersion),
                UpdateCount = updateCount
            };

            _logger.LogWarning("Concurrency conflict updating quest {QuestId}", questId);

            return ServiceResult<QuestDto>.Conflict(
                "Quest was modified by another user. Please reload and try again.",
                currentDto);
        }

        // Fetch update count for result
        var resultUpdateCount = await _context.QuestUpdates.CountAsync(qu => qu.QuestId == questId);

        var resultDto = new QuestDto
        {
            Id = quest.Id,
            ArcId = quest.ArcId,
            Title = quest.Title,
            Description = quest.Description,
            Status = quest.Status,
            IsGmOnly = quest.IsGmOnly,
            SortOrder = quest.SortOrder,
            CreatedBy = quest.CreatedBy,
            CreatedByName = quest.Creator.DisplayName,
            CreatedAt = quest.CreatedAt,
            UpdatedAt = quest.UpdatedAt,
            RowVersion = Convert.ToBase64String(quest.RowVersion),
            UpdateCount = resultUpdateCount
        };

        return ServiceResult<QuestDto>.Success(resultDto);
    }

    public async Task<ServiceResult<bool>> DeleteQuestAsync(Guid questId, Guid userId)
    {
        var quest = await _context.Quests
            .Include(q => q.Arc)
                .ThenInclude(a => a.Campaign)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
        {
            return ServiceResult<bool>.NotFound("Quest not found");
        }

        // Check world membership and GM role
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<bool>.NotFound("Quest not found");
        }

        if (member.Role != WorldRole.GM)
        {
            return ServiceResult<bool>.Forbidden("Only GMs can delete quests");
        }

        _context.Quests.Remove(quest);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Deleted quest {QuestId} and its updates for user {UserId}", questId, userId);

        return ServiceResult<bool>.Success(true);
    }
}
