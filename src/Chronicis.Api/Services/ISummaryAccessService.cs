namespace Chronicis.Api.Services;

public interface ISummaryAccessService
{
    Task<bool> CanAccessArticleAsync(Guid articleId, Guid userId);
    Task<bool> CanAccessCampaignAsync(Guid campaignId, Guid userId);
    Task<bool> CanAccessArcAsync(Guid arcId, Guid userId);
}

