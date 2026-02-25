using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class SessionService : ISessionService
{
    private readonly ChronicisDbContext _context;
    private readonly ISummaryService _summaryService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ChronicisDbContext context,
        ISummaryService summaryService,
        ILogger<SessionService> logger)
    {
        _context = context;
        _summaryService = summaryService;
        _logger = logger;
    }

    public async Task<ServiceResult<List<SessionTreeDto>>> GetSessionsByArcAsync(Guid arcId, Guid userId)
    {
        var arc = await _context.Arcs
            .AsNoTracking()
            .Include(a => a.Campaign)
                .ThenInclude(c => c.World)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            return ServiceResult<List<SessionTreeDto>>.NotFound("Arc not found");
        }

        var membership = arc.Campaign.World.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return ServiceResult<List<SessionTreeDto>>.NotFound("Arc not found or access denied");
        }

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.ArcId == arcId)
            .OrderBy(s => s.SessionDate ?? DateTime.MaxValue)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.CreatedAt)
            .Select(s => new SessionTreeDto
            {
                Id = s.Id,
                ArcId = s.ArcId,
                Name = s.Name,
                SessionDate = s.SessionDate,
                HasAiSummary = !string.IsNullOrWhiteSpace(s.AiSummary)
            })
            .ToListAsync();

        return ServiceResult<List<SessionTreeDto>>.Success(sessions);
    }

    public async Task<ServiceResult<SessionDto>> GetSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.Arc)
                .ThenInclude(a => a.Campaign)
                    .ThenInclude(c => c.World)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return ServiceResult<SessionDto>.NotFound("Session not found");
        }

        var membership = session.Arc.Campaign.World.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return ServiceResult<SessionDto>.NotFound("Session not found or access denied");
        }

        var dto = MapDto(session);

        // Server remains the source of truth for GM-only private notes visibility/editability.
        if (membership.Role != WorldRole.GM)
        {
            dto.PrivateNotes = null;
        }

        return ServiceResult<SessionDto>.Success(dto);
    }

    public async Task<ServiceResult<SessionDto>> CreateSessionAsync(Guid arcId, SessionCreateDto dto, Guid userId, string? username)
    {
        if (dto == null)
        {
            return ServiceResult<SessionDto>.ValidationError("Request body is required");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return ServiceResult<SessionDto>.ValidationError("Session name is required");
        }

        var trimmedName = dto.Name.Trim();
        if (trimmedName.Length > 500)
        {
            return ServiceResult<SessionDto>.ValidationError("Session name must be 500 characters or fewer");
        }

        var arc = await _context.Arcs
            .Include(a => a.Campaign)
                .ThenInclude(c => c.World)
                    .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
        {
            return ServiceResult<SessionDto>.NotFound("Arc not found");
        }

        var membership = arc.Campaign.World.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return ServiceResult<SessionDto>.NotFound("Arc not found or access denied");
        }

        if (membership.Role != WorldRole.GM)
        {
            return ServiceResult<SessionDto>.Forbidden("Only GMs can create sessions");
        }

        var utcNow = DateTime.UtcNow;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = trimmedName,
            SessionDate = dto.SessionDate,
            CreatedAt = utcNow,
            CreatedBy = userId
        };

        var noteTitle = BuildDefaultNoteTitle(username);
        var noteSlug = await GenerateUniqueRootSlugAsync(noteTitle, arc.Campaign.WorldId);

        var defaultNote = new Article
        {
            Id = Guid.NewGuid(),
            Title = noteTitle,
            Slug = noteSlug,
            Body = null,
            Type = ArticleType.SessionNote,
            Visibility = ArticleVisibility.Public,
            SessionId = session.Id,
            WorldId = arc.Campaign.WorldId,
            CampaignId = arc.CampaignId,
            ArcId = arc.Id,
            ParentId = null,
            CreatedBy = userId,
            CreatedAt = utcNow,
            EffectiveDate = utcNow
        };

        _context.Sessions.Add(session);
        _context.Articles.Add(defaultNote);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created session {SessionId} in arc {ArcId} with default note {NoteId}",
            session.Id, arc.Id, defaultNote.Id);

        return ServiceResult<SessionDto>.Success(MapDto(session));
    }

    public async Task<ServiceResult<SessionDto>> UpdateSessionNotesAsync(Guid sessionId, SessionUpdateDto dto, Guid userId)
    {
        if (dto == null)
        {
            return ServiceResult<SessionDto>.ValidationError("Request body is required");
        }

        if (dto.ClearSessionDate && dto.SessionDate.HasValue)
        {
            return ServiceResult<SessionDto>.ValidationError("Session date and ClearSessionDate cannot both be set");
        }

        string? trimmedName = null;
        if (dto.Name != null)
        {
            trimmedName = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return ServiceResult<SessionDto>.ValidationError("Session name is required");
            }

            if (trimmedName.Length > 500)
            {
                return ServiceResult<SessionDto>.ValidationError("Session name must be 500 characters or fewer");
            }
        }

        var session = await _context.Sessions
            .Include(s => s.Arc)
                .ThenInclude(a => a.Campaign)
                    .ThenInclude(c => c.World)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return ServiceResult<SessionDto>.NotFound("Session not found");
        }

        var membership = session.Arc.Campaign.World.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return ServiceResult<SessionDto>.NotFound("Session not found or access denied");
        }

        if (membership.Role != WorldRole.GM)
        {
            return ServiceResult<SessionDto>.Forbidden("Only GMs can update session notes");
        }

        if (trimmedName != null)
        {
            session.Name = trimmedName;
        }

        if (dto.ClearSessionDate)
        {
            session.SessionDate = null;
        }
        else if (dto.SessionDate.HasValue)
        {
            session.SessionDate = dto.SessionDate;
        }

        session.PublicNotes = dto.PublicNotes;
        session.PrivateNotes = dto.PrivateNotes;
        session.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated session {SessionId}", sessionId);

        return ServiceResult<SessionDto>.Success(MapDto(session));
    }

    public async Task<ServiceResult<SummaryGenerationDto>> GenerateAiSummaryAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.Sessions
            .Include(s => s.Arc)
                .ThenInclude(a => a.Campaign)
                    .ThenInclude(c => c.World)
                        .ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return ServiceResult<SummaryGenerationDto>.NotFound("Session not found");
        }

        var membership = session.Arc.Campaign.World.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return ServiceResult<SummaryGenerationDto>.NotFound("Session not found or access denied");
        }

        var summarySources = new List<SummarySourceDto>();
        var sourceBlocks = new List<string>();

        if (!string.IsNullOrWhiteSpace(session.PublicNotes))
        {
            sourceBlocks.Add($"--- From: {session.Name} (Public Notes) ---\n{session.PublicNotes}\n---");
            summarySources.Add(new SummarySourceDto
            {
                Type = "SessionPublicNotes",
                Title = $"{session.Name} (Public Notes)"
            });
        }

        // Security rule: source filtering is fixed and caller-independent. Only Public SessionNote bodies are allowed.
        var publicSessionNotes = await _context.Articles
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId
                && a.Type == ArticleType.SessionNote
                && a.Visibility == ArticleVisibility.Public
                && !string.IsNullOrEmpty(a.Body))
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Body
            })
            .ToListAsync();

        foreach (var note in publicSessionNotes)
        {
            sourceBlocks.Add($"--- From: {note.Title} (SessionNote) ---\n{note.Body}\n---");
            summarySources.Add(new SummarySourceDto
            {
                Type = "SessionNote",
                Title = note.Title ?? "Session Note",
                ArticleId = note.Id
            });
        }

        if (sourceBlocks.Count == 0)
        {
            return ServiceResult<SummaryGenerationDto>.ValidationError("No public content available for this session.");
        }

        var sourceContent = string.Join("\n\n", sourceBlocks);
        var generation = await _summaryService.GenerateSessionSummaryFromSourcesAsync(
            session.Name,
            sourceContent,
            summarySources);

        if (!generation.Success)
        {
            return ServiceResult<SummaryGenerationDto>.ValidationError(
                generation.ErrorMessage ?? "Error generating session summary");
        }

        var generatedAt = DateTime.UtcNow;
        session.AiSummary = generation.Summary;
        session.AiSummaryGeneratedAt = generatedAt;
        session.AiSummaryGeneratedByUserId = userId;

        await _context.SaveChangesAsync();

        generation.GeneratedDate = generatedAt;

        _logger.LogDebug("Generated AI summary for session {SessionId}", sessionId);

        return ServiceResult<SummaryGenerationDto>.Success(generation);
    }

    private async Task<string> GenerateUniqueRootSlugAsync(string title, Guid worldId)
    {
        var baseSlug = SlugGenerator.GenerateSlug(title);
        var existingSlugs = await _context.Articles
            .AsNoTracking()
            .Where(a => a.WorldId == worldId && a.ParentId == null)
            .Select(a => a.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
    }

    private static string BuildDefaultNoteTitle(string? username)
    {
        var trimmed = username?.Trim();
        var title = string.IsNullOrWhiteSpace(trimmed)
            ? "My Notes"
            : $"{trimmed}'s Notes";

        return title.Length <= 500 ? title : title[..500];
    }

    private static SessionDto MapDto(Session session)
    {
        return new SessionDto
        {
            Id = session.Id,
            ArcId = session.ArcId,
            Name = session.Name,
            SessionDate = session.SessionDate,
            PublicNotes = session.PublicNotes,
            PrivateNotes = session.PrivateNotes,
            AiSummary = session.AiSummary,
            AiSummaryGeneratedAt = session.AiSummaryGeneratedAt,
            AiSummaryGeneratedByUserId = session.AiSummaryGeneratedByUserId,
            CreatedAt = session.CreatedAt,
            ModifiedAt = session.ModifiedAt,
            CreatedBy = session.CreatedBy
        };
    }
}
