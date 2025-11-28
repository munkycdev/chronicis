using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

public class CreateArticle : ArticleBaseClass
{
    private readonly ArticleValidationService _validationService;

    public CreateArticle(ChronicisDbContext context, ArticleValidationService validationService) : base(context)
    {
        _validationService = validationService;
    }

    [Function("CreateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles")] HttpRequestData req)
    {
        try
        {
            // Parse request body
            var dto = await JsonSerializer.DeserializeAsync<ArticleCreateDto>(req.Body, _options);

            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            // Validate
            var validationResult = await _validationService.ValidateCreateAsync(dto);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteAsJsonAsync(new { errors = validationResult.Errors });
                return validationError;
            }

            // Create article
            var article = new Article
            {
                Title = dto.Title,
                ParentId = dto.ParentId,
                Body = dto.Body,
                CreatedDate = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Load parent if exists for response
            if (article.ParentId.HasValue)
            {
                await _context.Entry(article)
                    .Reference(a => a.Parent)
                    .LoadAsync();
            }

            // Map to DTO
            var responseDto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                ParentId = article.ParentId,
                Body = article.Body,
                CreatedDate = article.CreatedDate,
                ModifiedDate = article.ModifiedDate,
                HasChildren = false // New article has no children
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(responseDto);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error creating article: {ex.Message}");
            return errorResponse;
        }
    }
}