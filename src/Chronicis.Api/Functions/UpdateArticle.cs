using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

public class UpdateArticle
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleValidationService _validationService;
    private readonly IHashtagSyncService _hashtagSync;
    private readonly ILogger<UpdateArticle> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateArticle(
        ChronicisDbContext context, 
        IArticleValidationService validationService, 
        IHashtagSyncService hashtagSync,
        ILogger<UpdateArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _hashtagSync = hashtagSync;
        _logger = logger;
    }

    [Function("UpdateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "articles/{id}")] HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("UpdateArticle {ArticleId} called by user {UserId}", id, user.Id);

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
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            article.Title = dto.Title;
            article.Body = dto.Body;
            article.ModifiedDate = DateTime.UtcNow;
            article.IconEmoji = dto.IconEmoji;
            
            if (dto.EffectiveDate.HasValue)
            {
                article.EffectiveDate = dto.EffectiveDate.Value;
            }

            await _context.SaveChangesAsync();
            await _hashtagSync.SyncHashtagsAsync(article.Id, article.Body);

            var responseDto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                Body = article.Body,
                CreatedDate = article.CreatedDate,
                ModifiedDate = article.ModifiedDate,
                EffectiveDate = article.EffectiveDate,
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
