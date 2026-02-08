using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for QuestUpdate operations with world membership authorization.
/// </summary>
public class QuestUpdateService : IQuestUpdateService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<QuestUpdateService> _logger;

    public QuestUpdateService(ChronicisDbContext context, ILogger<QuestUpdateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<QuestUpdateEntryDto>>> GetQuestUpdatesAsync(
        Guid questId, 
        Guid userId, 
        int skip = 0, 
        int take = 20)
    {
        // Validate pagination parameters
        if (skip < 0)
        {
            return ServiceResult<PagedResult<QuestUpdateEntryDto>>.ValidationError("Skip must be non-negative");
        }

        if (take < 1 || take > 100)
        {
            return ServiceResult<PagedResult<QuestUpdateEntryDto>>.ValidationError("Take must be between 1 and 100");
        }

        // Find quest and check access
        var quest = await _context.Quests
            .AsNoTracking()
            .Include(q => q.Arc)
                .ThenInclude(a => a.Campaign)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
        {
            return ServiceResult<PagedResult<QuestUpdateEntryDto>>.NotFound("Quest not found");
        }

        // Check world membership
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<PagedResult<QuestUpdateEntryDto>>.NotFound("Quest not found");
        }

        var isGM = member.Role == WorldRole.GM;

        // Non-GM users cannot see GM-only quests
        if (quest.IsGmOnly && !isGM)
        {
            return ServiceResult<PagedResult<QuestUpdateEntryDto>>.NotFound("Quest not found");
        }

        // Get total count
        var totalCount = await _context.QuestUpdates
            .CountAsync(qu => qu.QuestId == questId);

        // Get paginated updates
        var updates = await _context.QuestUpdates
            .AsNoTracking()
            .Where(qu => qu.QuestId == questId)
            .OrderByDescending(qu => qu.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(qu => new QuestUpdateEntryDto
            {
                Id = qu.Id,
                QuestId = qu.QuestId,
                Body = qu.Body,
                SessionId = qu.SessionId,
                SessionTitle = qu.Session != null ? qu.Session.Title : null,
                CreatedBy = qu.CreatedBy,
                CreatedByName = qu.Creator.DisplayName,
                CreatedByAvatarUrl = qu.Creator.AvatarUrl,
                CreatedAt = qu.CreatedAt
            })
            .ToListAsync();

        var result = new PagedResult<QuestUpdateEntryDto>
        {
            Items = updates,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };

        return ServiceResult<PagedResult<QuestUpdateEntryDto>>.Success(result);
    }

    public async Task<ServiceResult<QuestUpdateEntryDto>> CreateQuestUpdateAsync(
        Guid questId, 
        QuestUpdateCreateDto dto, 
        Guid userId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Body))
        {
            return ServiceResult<QuestUpdateEntryDto>.ValidationError("Body is required and cannot be empty");
        }

        // Find quest and check access
        var quest = await _context.Quests
            .Include(q => q.Arc)
                .ThenInclude(a => a.Campaign)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
        {
            return ServiceResult<QuestUpdateEntryDto>.NotFound("Quest not found");
        }

        // Check world membership and role
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<QuestUpdateEntryDto>.NotFound("Quest not found");
        }

        // Observer cannot create updates
        if (member.Role == WorldRole.Observer)
        {
            return ServiceResult<QuestUpdateEntryDto>.Forbidden("Observers cannot create quest updates");
        }

        var isGM = member.Role == WorldRole.GM;

        // Non-GM users cannot update GM-only quests
        if (quest.IsGmOnly && !isGM)
        {
            return ServiceResult<QuestUpdateEntryDto>.NotFound("Quest not found");
        }

        // Validate SessionId if provided
        Article? session = null;
        if (dto.SessionId.HasValue)
        {
            session = await _context.Articles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == dto.SessionId.Value);

            if (session == null)
            {
                return ServiceResult<QuestUpdateEntryDto>.ValidationError("Session not found");
            }

            if (session.Type != ArticleType.Session)
            {
                return ServiceResult<QuestUpdateEntryDto>.ValidationError("Referenced article is not a Session");
            }

            if (session.ArcId != quest.ArcId)
            {
                return ServiceResult<QuestUpdateEntryDto>.ValidationError("Session must belong to the same Arc as the quest");
            }
        }

        var now = DateTime.UtcNow;

        var questUpdate = new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = questId,
            SessionId = dto.SessionId,
            Body = dto.Body.Trim(),
            CreatedBy = userId,
            CreatedAt = now
        };

        _context.QuestUpdates.Add(questUpdate);

        // Update parent Quest's UpdatedAt timestamp
        quest.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Created quest update for quest {QuestId} by user {UserId}", questId, userId);

        // Fetch creator details for DTO
        var creator = await _context.Users.FindAsync(userId);

        var resultDto = new QuestUpdateEntryDto
        {
            Id = questUpdate.Id,
            QuestId = questUpdate.QuestId,
            Body = questUpdate.Body,
            SessionId = questUpdate.SessionId,
            SessionTitle = session?.Title,
            CreatedBy = questUpdate.CreatedBy,
            CreatedByName = creator?.DisplayName ?? "Unknown",
            CreatedByAvatarUrl = creator?.AvatarUrl,
            CreatedAt = questUpdate.CreatedAt
        };

        return ServiceResult<QuestUpdateEntryDto>.Success(resultDto);
    }

    public async Task<ServiceResult<bool>> DeleteQuestUpdateAsync(
        Guid questId, 
        Guid updateId, 
        Guid userId)
    {
        // Find the quest update
        var questUpdate = await _context.QuestUpdates
            .Include(qu => qu.Quest)
                .ThenInclude(q => q.Arc)
                    .ThenInclude(a => a.Campaign)
            .FirstOrDefaultAsync(qu => qu.Id == updateId && qu.QuestId == questId);

        if (questUpdate == null)
        {
            return ServiceResult<bool>.NotFound("Quest update not found");
        }

        // Check world membership and role
        var member = await _context.WorldMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorldId == questUpdate.Quest.Arc.Campaign.WorldId && m.UserId == userId);

        if (member == null)
        {
            return ServiceResult<bool>.NotFound("Quest update not found");
        }

        var isGM = member.Role == WorldRole.GM;

        // Determine if user can delete this update
        bool canDelete = isGM || questUpdate.CreatedBy == userId;

        if (!canDelete)
        {
            return ServiceResult<bool>.Forbidden("You can only delete your own quest updates");
        }

        _context.QuestUpdates.Remove(questUpdate);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Deleted quest update {UpdateId} from quest {QuestId} by user {UserId}", 
            updateId, questId, userId);

        return ServiceResult<bool>.Success(true);
    }
}
