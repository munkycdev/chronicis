using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Dashboard data.
/// </summary>
[ApiController]
[Route("dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly IPromptService _promptService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ChronicisDbContext context,
        IPromptService promptService,
        ICurrentUserService currentUserService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _promptService = promptService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/dashboard - Get aggregated dashboard data for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting dashboard for user {UserId}", user.Id);

        // Get all worlds the user has access to (via membership)
        var worldIds = await _context.WorldMembers
            .Where(wm => wm.UserId == user.Id)
            .Select(wm => wm.WorldId)
            .ToListAsync();

        // Get world data with campaigns and arcs
        var worlds = await _context.Worlds
            .Where(w => worldIds.Contains(w.Id))
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
            .Include(w => w.Articles)
            .OrderBy(w => w.Name)
            .ToListAsync();

        // Get claimed characters for this user across all worlds
        var claimedCharacters = await _context.Articles
            .Where(a => a.PlayerId == user.Id && a.Type == ArticleType.Character)
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

        // Build dashboard worlds
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

            // Find the world root article (if exists)
            var worldRoot = world.Articles?.FirstOrDefault(a => a.ParentId == null && a.Type == ArticleType.WikiArticle);
            dashboardWorld.WorldRootArticleId = worldRoot?.Id;

            // Build campaign data
            if (world.Campaigns != null)
            {
                foreach (var campaign in world.Campaigns.OrderByDescending(c => c.IsActive).ThenBy(c => c.Name))
                {
                    var sessionCount = world.Articles?.Count(a => a.CampaignId == campaign.Id && a.Type == ArticleType.Session) ?? 0;

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

                    // Get current/active arc
                    var activeArc = campaign.Arcs?
                        .Where(a => a.IsActive)
                        .OrderByDescending(a => a.SortOrder)
                        .FirstOrDefault();

                    if (activeArc != null)
                    {
                        var arcSessionCount = world.Articles?.Count(a => a.ArcId == activeArc.Id && a.Type == ArticleType.Session) ?? 0;
                        var latestSession = world.Articles?
                            .Where(a => a.ArcId == activeArc.Id && a.Type == ArticleType.Session)
                            .OrderByDescending(a => a.SessionDate ?? a.CreatedAt)
                            .FirstOrDefault();

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

            // Get user's characters in this world
            var myCharactersInWorld = claimedCharacters
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

            dashboardWorld.MyCharacters = myCharactersInWorld;

            dashboardWorlds.Add(dashboardWorld);
        }

        var dashboard = new DashboardDto
        {
            UserDisplayName = user.DisplayName,
            Worlds = dashboardWorlds,
            ClaimedCharacters = claimedCharacters,
            Prompts = new List<PromptDto>()
        };

        // Generate contextual prompts
        dashboard.Prompts = _promptService.GeneratePrompts(dashboard);

        return Ok(dashboard);
    }
}
