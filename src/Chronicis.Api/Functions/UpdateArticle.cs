using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

public class UpdateArticle : ArticleBaseClass
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleValidationService _validationService;

    public UpdateArticle(ChronicisDbContext context, ArticleValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    [Function("UpdateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "articles/{id}")] HttpRequestData req,
        int id)
    {
        try
        {
            // Parse request body
            var dto = await JsonSerializer.DeserializeAsync<ArticleUpdateDto>(req.Body, _options);
            
            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            // Validate
            var validationResult = await _validationService.ValidateUpdateAsync(id, dto);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteAsJsonAsync(new { errors = validationResult.Errors });
                return validationError;
            }

            // Get existing article
            var article = await _context.Articles
                .Include(a => a.Children)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            // Update article
            article.Title = dto.Title;
            article.Body = dto.Body;
            article.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Map to DTO
            var responseDto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                Body = article.Body,
                CreatedDate = article.CreatedDate,
                ModifiedDate = article.ModifiedDate,
                HasChildren = article.Children.Any()
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseDto);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error updating article: {ex.Message}");
            return errorResponse;
        }
    }
}
