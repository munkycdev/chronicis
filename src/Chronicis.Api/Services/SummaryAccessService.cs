using Chronicis.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class SummaryAccessService : ISummaryAccessService
{
    private readonly ChronicisDbContext _context;

    public SummaryAccessService(ChronicisDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessArticleAsync(Guid articleId, Guid userId)
    {
        return await _context.Articles
            .Where(a => a.Id == articleId)
            .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId))
            .AnyAsync();
    }

    public async Task<bool> CanAccessCampaignAsync(Guid campaignId, Guid userId)
    {
        return await _context.Campaigns
            .Where(c => c.Id == campaignId)
            .Where(c => c.World != null && c.World.Members.Any(m => m.UserId == userId))
            .AnyAsync();
    }

    public async Task<bool> CanAccessArcAsync(Guid arcId, Guid userId)
    {
        return await _context.Arcs
            .Where(a => a.Id == arcId)
            .Where(a => a.Campaign != null && a.Campaign.World != null && a.Campaign.World.Members.Any(m => m.UserId == userId))
            .AnyAsync();
    }
}

