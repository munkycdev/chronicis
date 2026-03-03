using System.Linq.Expressions;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

internal static class ArticleReadModelProjection
{
    internal static readonly Expression<Func<Article, ArticleDto>> ArticleDetail = a => new ArticleDto
    {
        Id = a.Id,
        Title = a.Title,
        Slug = a.Slug,
        ParentId = a.ParentId,
        WorldId = a.WorldId,
        CampaignId = a.CampaignId,
        ArcId = a.ArcId,
        SessionId = a.SessionId,
        Body = a.Body ?? string.Empty,
        Type = a.Type,
        Visibility = a.Visibility,
        CreatedAt = a.CreatedAt,
        ModifiedAt = a.ModifiedAt,
        EffectiveDate = a.EffectiveDate,
        CreatedBy = a.CreatedBy,
        LastModifiedBy = a.LastModifiedBy,
        IconEmoji = a.IconEmoji,
        SessionDate = a.SessionDate,
        InGameDate = a.InGameDate,
        PlayerId = a.PlayerId,
        AISummary = a.AISummary,
        AISummaryGeneratedAt = a.AISummaryGeneratedAt,
        Breadcrumbs = new List<BreadcrumbDto>(),
        Aliases = new List<ArticleAliasDto>()
    };
}
