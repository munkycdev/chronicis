using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class UpdateArticle
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleValidationService _validationService;
    private readonly IArticleService _articleService;
    private readonly ILinkSyncService _linkSyncService;
    private readonly ILogger<UpdateArticle> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateArticle(
        ChronicisDbContext context,
        IArticleValidationService validationService,
        IArticleService articleService,
        ILinkSyncService linkSyncService,
        ILogger<UpdateArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _articleService = articleService;
        _linkSyncService = linkSyncService;
        _logger = logger;
    }

    [Function("UpdateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "articles/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var dto = await JsonSerializer.DeserializeAsync<ArticleUpdateDto>(req.Body, _jsonOptions);

            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            var validationResult = await _validationService.ValidateUpdateAsync(id, dto);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteAsJsonAsync(new { errors = validationResult.Errors });
                return validationError;
            }

            var article = await _context.Articles
                .Include(a => a.Children)
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == user.Id);

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            article.Title = dto.Title;
            article.Body = dto.Body;
            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = user.Id;
            article.IconEmoji = dto.IconEmoji;

            if (dto.EffectiveDate.HasValue)
            {
                article.EffectiveDate = dto.EffectiveDate.Value;
            }

            if (dto.Visibility.HasValue)
            {
                article.Visibility = dto.Visibility.Value;
            }

            if (dto.Type.HasValue)
            {
                article.Type = dto.Type.Value;
                _logger.LogInformation("Updated article {ArticleId} type to {ArticleType}", id, dto.Type.Value);
            }

            if (dto.SessionDate.HasValue)
            {
                article.SessionDate = dto.SessionDate.Value;
            }

            if (!string.IsNullOrEmpty(dto.InGameDate))
            {
                article.InGameDate = dto.InGameDate;
            }

            // Handle slug update if provided
            if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != article.Slug)
            {
                // Validate custom slug
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    var badSlug = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badSlug.WriteStringAsync("Slug must contain only lowercase letters, numbers, and hyphens");
                    return badSlug;
                }

                // Check uniqueness (excluding current article)
                if (!await _articleService.IsSlugUniqueAsync(dto.Slug, article.ParentId, user.Id, article.Id))
                {
                    var duplicateSlug = req.CreateResponse(HttpStatusCode.Conflict);
                    await duplicateSlug.WriteStringAsync($"An article with slug '{dto.Slug}' already exists in this location");
                    return duplicateSlug;
                }

                article.Slug = dto.Slug;
            }

            await _context.SaveChangesAsync();

            // Sync wiki links after saving article
            await _linkSyncService.SyncLinksAsync(article.Id, article.Body);

            var responseDto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,
                ParentId = article.ParentId,
                WorldId = article.WorldId,
                CampaignId = article.CampaignId,
                ArcId = article.ArcId,
                Body = article.Body ?? string.Empty,
                Type = article.Type,
                Visibility = article.Visibility,
                CreatedAt = article.CreatedAt,
                ModifiedAt = article.ModifiedAt,
                EffectiveDate = article.EffectiveDate,
                CreatedBy = article.CreatedBy,
                LastModifiedBy = article.LastModifiedBy,
                IconEmoji = article.IconEmoji,
                HasChildren = article?.Children?.Count > 0
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseDto);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article {ArticleId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error updating article: {ex.Message}");
            return errorResponse;
        }
    }
}
