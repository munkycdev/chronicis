using Chronicis.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class SummaryAccessService : ISummaryAccessService
{
    private readonly ChronicisDbContext _context;
    private readonly IReadAccessPolicyService _readAccessPolicy;

    public SummaryAccessService(ChronicisDbContext context, IReadAccessPolicyService readAccessPolicy)
    {
        _context = context;
        _readAccessPolicy = readAccessPolicy;
    }

    public async Task<bool> CanAccessArticleAsync(Guid articleId, Guid userId)
    {
        return await _readAccessPolicy
            .ApplyAuthenticatedWorldArticleFilter(_context.Articles, userId)
            .Where(a => a.Id == articleId)
            .AnyAsync();
    }

    public async Task<bool> CanAccessCampaignAsync(Guid campaignId, Guid userId)
    {
        return await _readAccessPolicy
            .ApplyAuthenticatedCampaignFilter(_context.Campaigns, userId)
            .Where(c => c.Id == campaignId)
            .AnyAsync();
    }

    public async Task<bool> CanAccessArcAsync(Guid arcId, Guid userId)
    {
        return await _readAccessPolicy
            .ApplyAuthenticatedArcFilter(_context.Arcs, userId)
            .Where(a => a.Id == arcId)
            .AnyAsync();
    }
}

