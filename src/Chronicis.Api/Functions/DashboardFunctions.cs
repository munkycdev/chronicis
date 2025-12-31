using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for dashboard data.
/// </summary>
public class DashboardFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<DashboardFunctions> _logger;

    public DashboardFunctions(ChronicisDbContext context, ILogger<DashboardFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets aggregated dashboard data for the current user.
    /// </summary>
    [Function("GetDashboard")]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Get user's worlds with campaigns
        var worlds = await _context.Worlds
            .Where(w => w.OwnerId == user.Id)
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        // Get user's claimed characters
        var claimedCharacters = await _context.Articles
            .Where(a => a.PlayerId == user.Id && a.Type == ArticleType.Character)
            .Include(a => a.World)
            .ToListAsync();

        // Get article counts per world
        var worldIds = worlds.Select(w => w.Id).ToList();
        var articleCounts = await _context.Articles
            .Where(a => a.WorldId.HasValue && worldIds.Contains(a.WorldId.Value))
            .GroupBy(a => a.WorldId)
            .Select(g => new { WorldId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WorldId!.Value, x => x.Count);

        // Get session counts per campaign
        var campaignIds = worlds.SelectMany(w => w.Campaigns).Select(c => c.Id).ToList();
        var sessionCounts = await _context.Articles
            .Where(a => a.CampaignId.HasValue && campaignIds.Contains(a.CampaignId.Value) && a.Type == ArticleType.Session)
            .GroupBy(a => a.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CampaignId!.Value, x => x.Count);

        // Get latest session dates per arc
        var arcIds = worlds.SelectMany(w => w.Campaigns).SelectMany(c => c.Arcs).Select(a => a.Id).ToList();
        var latestSessionDates = await _context.Articles
            .Where(a => a.ArcId.HasValue && arcIds.Contains(a.ArcId.Value) && a.Type == ArticleType.Session && a.SessionDate.HasValue)
            .GroupBy(a => a.ArcId)
            .Select(g => new { ArcId = g.Key, LatestDate = g.Max(a => a.SessionDate) })
            .ToDictionaryAsync(x => x.ArcId!.Value, x => x.LatestDate);

        // Get session counts per arc
        var arcSessionCounts = await _context.Articles
            .Where(a => a.ArcId.HasValue && arcIds.Contains(a.ArcId.Value) && a.Type == ArticleType.Session)
            .GroupBy(a => a.ArcId)
            .Select(g => new { ArcId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ArcId!.Value, x => x.Count);

        // Get world root article IDs (first root-level article per world)
        var worldRootArticles = await _context.Articles
            .Where(a => a.WorldId.HasValue && worldIds.Contains(a.WorldId.Value) && a.ParentId == null)
            .GroupBy(a => a.WorldId)
            .Select(g => new { WorldId = g.Key, ArticleId = g.OrderBy(a => a.CreatedAt).First().Id })
            .ToDictionaryAsync(x => x.WorldId!.Value, x => x.ArticleId);

        // Build dashboard DTO
        var dashboard = new DashboardDto
        {
            UserDisplayName = user.DisplayName,
            Worlds = worlds.Select(w => new DashboardWorldDto
            {
                Id = w.Id,
                Name = w.Name,
                Slug = w.Slug,
                Description = w.Description,
                CreatedAt = w.CreatedAt,
                WorldRootArticleId = worldRootArticles.GetValueOrDefault(w.Id),
                ArticleCount = articleCounts.GetValueOrDefault(w.Id, 0),
                Campaigns = w.Campaigns
                    .OrderByDescending(c => c.IsActive)
                    .ThenByDescending(c => c.CreatedAt)
                    .Select(c => new DashboardCampaignDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        StartedAt = c.StartedAt,
                        IsActive = c.IsActive,
                        SessionCount = sessionCounts.GetValueOrDefault(c.Id, 0),
                        ArcCount = c.Arcs.Count,
                        CurrentArc = c.Arcs
                            .OrderByDescending(a => a.SortOrder)
                            .Select(a => new DashboardArcDto
                            {
                                Id = a.Id,
                                Name = a.Name,
                                Description = a.Description,
                                SessionCount = arcSessionCounts.GetValueOrDefault(a.Id, 0),
                                LatestSessionDate = latestSessionDates.GetValueOrDefault(a.Id)
                            })
                            .FirstOrDefault()
                    })
                    .ToList(),
                MyCharacters = claimedCharacters
                    .Where(ch => ch.WorldId == w.Id)
                    .Select(ch => new DashboardCharacterDto
                    {
                        Id = ch.Id,
                        Title = ch.Title,
                        IconEmoji = ch.IconEmoji,
                        ModifiedAt = ch.ModifiedAt,
                        CreatedAt = ch.CreatedAt
                    })
                    .ToList()
            }).ToList(),
            ClaimedCharacters = claimedCharacters.Select(ch => new ClaimedCharacterDto
            {
                Id = ch.Id,
                Title = ch.Title,
                IconEmoji = ch.IconEmoji,
                WorldId = ch.WorldId ?? Guid.Empty,
                WorldName = ch.World?.Name ?? "Unknown",
                ModifiedAt = ch.ModifiedAt,
                CreatedAt = ch.CreatedAt
            }).ToList()
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dashboard);
        return response;
    }
}
