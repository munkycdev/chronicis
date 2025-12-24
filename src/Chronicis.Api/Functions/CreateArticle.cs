using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Function for creating new articles.
/// </summary>
public class CreateArticle
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleValidationService _validationService;
    private readonly IArticleService _articleService;
    private readonly ILogger<CreateArticle> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CreateArticle(
        ChronicisDbContext context,
        IArticleValidationService validationService,
        IArticleService articleService,
        ILogger<CreateArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/articles - Creates a new article.
    /// </summary>
    [Function("CreateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        try
        {
            var dto = await JsonSerializer.DeserializeAsync<ArticleCreateDto>(req.Body, _jsonOptions);

            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            var validationResult = await _validationService.ValidateCreateAsync(dto);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteAsJsonAsync(new { errors = validationResult.Errors });
                return validationError;
            }

            // Generate slug (user can provide custom slug or we auto-generate)
            string slug;
            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                // Validate custom slug
                if (!SlugGenerator.IsValidSlug(dto.Slug))
                {
                    var badSlug = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badSlug.WriteStringAsync("Slug must contain only lowercase letters, numbers, and hyphens");
                    return badSlug;
                }

                // Check uniqueness
                if (!await _articleService.IsSlugUniqueAsync(dto.Slug, dto.ParentId, user.Id))
                {
                    var duplicateSlug = req.CreateResponse(HttpStatusCode.Conflict);
                    await duplicateSlug.WriteStringAsync($"An article with slug '{dto.Slug}' already exists in this location");
                    return duplicateSlug;
                }

                slug = dto.Slug;
            }
            else
            {
                // Auto-generate unique slug from title
                slug = await _articleService.GenerateUniqueSlugAsync(dto.Title, dto.ParentId, user.Id);
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Slug = slug,
                ParentId = dto.ParentId,
                WorldId = dto.WorldId,
                CampaignId = dto.CampaignId,
                ArcId = dto.ArcId,
                Body = dto.Body,
                Type = dto.Type,
                Visibility = dto.Visibility,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                EffectiveDate = dto.EffectiveDate ?? DateTime.UtcNow,
                IconEmoji = dto.IconEmoji,
                SessionDate = dto.SessionDate,
                InGameDate = dto.InGameDate,
                PlayerId = dto.PlayerId
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            if (article.ParentId.HasValue)
            {
                await _context.Entry(article).Reference(a => a.Parent).LoadAsync();
            }

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
                IconEmoji = article.IconEmoji,
                HasChildren = false
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(responseDto);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error creating article: {ex.Message}");
            return errorResponse;
        }
    }
}
