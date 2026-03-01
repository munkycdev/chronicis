using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class DashboardReadService : IDashboardReadService
{
    private readonly ChronicisDbContext _context;
    private readonly IPromptService _promptService;

    public DashboardReadService(ChronicisDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId, string userDisplayName)
    {
        var worldIds = await _context.WorldMembers
            .Where(wm => wm.UserId == userId)
            .Select(wm => wm.WorldId)
            .ToListAsync();

        var worlds = await _context.Worlds
            .Where(w => worldIds.Contains(w.Id))
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
            .Include(w => w.Articles)
            .OrderBy(w => w.Name)
            .ToListAsync();

        var claimedCharacters = await _context.Articles
            .Where(a => a.PlayerId == userId && a.Type == ArticleType.Character)
            .Where(a => a.WorldId.HasValue && worldIds.Contains(a.WorldId.Value))
            .Select(a => new ClaimedCharacterDto
            {
                Id = a.Id,
                Title = a.Title ?? "Unnamed Character",
                IconEmoji = a.IconEmoji,
                WorldId = a.WorldId!.Value,
                WorldName = a.World != null ? a.World.Name : "Unknown World",
                ModifiedAt = a.ModifiedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var dashboardWorlds = new List<DashboardWorldDto>();
        foreach (var world in worlds)
        {
            var dashboardWorld = new DashboardWorldDto
            {
                Id = world.Id,
                Name = world.Name,
                Slug = world.Slug,
                Description = world.Description,
                CreatedAt = world.CreatedAt,
                ArticleCount = world.Articles?.Count ?? 0,
                Campaigns = new List<DashboardCampaignDto>(),
                MyCharacters = new List<DashboardCharacterDto>()
            };

            var worldRoot = world.Articles?.FirstOrDefault(a => a.ParentId == null && a.Type == ArticleType.WikiArticle);
            dashboardWorld.WorldRootArticleId = worldRoot?.Id;

            if (world.Campaigns != null)
            {
                foreach (var campaign in world.Campaigns.OrderByDescending(c => c.IsActive).ThenBy(c => c.Name))
                {
                    var sessionCount = await _context.Sessions
                        .AsNoTracking()
                        .CountAsync(s => s.Arc.CampaignId == campaign.Id);

                    var dashboardCampaign = new DashboardCampaignDto
                    {
                        Id = campaign.Id,
                        Name = campaign.Name,
                        Description = campaign.Description,
                        CreatedAt = campaign.CreatedAt,
                        StartedAt = campaign.StartedAt,
                        IsActive = campaign.IsActive,
                        SessionCount = sessionCount,
                        ArcCount = campaign.Arcs?.Count ?? 0
                    };

                    var activeArc = campaign.Arcs?
                        .Where(a => a.IsActive)
                        .OrderByDescending(a => a.SortOrder)
                        .FirstOrDefault();

                    if (activeArc != null)
                    {
                        var arcSessionCount = await _context.Sessions
                            .AsNoTracking()
                            .CountAsync(s => s.ArcId == activeArc.Id);

                        var latestSession = await _context.Sessions
                            .AsNoTracking()
                            .Where(s => s.ArcId == activeArc.Id)
                            .OrderByDescending(s => s.SessionDate ?? s.CreatedAt)
                            .FirstOrDefaultAsync();

                        dashboardCampaign.CurrentArc = new DashboardArcDto
                        {
                            Id = activeArc.Id,
                            Name = activeArc.Name,
                            Description = activeArc.Description,
                            SessionCount = arcSessionCount,
                            LatestSessionDate = latestSession?.SessionDate ?? latestSession?.CreatedAt
                        };
                    }

                    dashboardWorld.Campaigns.Add(dashboardCampaign);
                }
            }

            dashboardWorld.MyCharacters = claimedCharacters
                .Where(c => c.WorldId == world.Id)
                .Select(c => new DashboardCharacterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    IconEmoji = c.IconEmoji,
                    ModifiedAt = c.ModifiedAt,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            dashboardWorlds.Add(dashboardWorld);
        }

        var dashboard = new DashboardDto
        {
            UserDisplayName = userDisplayName,
            Worlds = dashboardWorlds,
            ClaimedCharacters = claimedCharacters,
            Prompts = new List<PromptDto>()
        };

        dashboard.Prompts = _promptService.GeneratePrompts(dashboard);
        return dashboard;
    }
}

