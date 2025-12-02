using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class CreateArticle
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleValidationService _validationService;
    private readonly ILogger<CreateArticle> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CreateArticle(
        ChronicisDbContext context,
        IArticleValidationService validationService,
        ILogger<CreateArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _logger = logger;
    }

    [Function("CreateArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("CreateArticle called by user {UserId}", user.Id);

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

            var article = new Article
            {
                Title = dto.Title,
                ParentId = dto.ParentId,
                Body = dto.Body,
                CreatedDate = DateTime.UtcNow,
                UserId = user.Id
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
                ParentId = article.ParentId,
                Body = article.Body,
                CreatedDate = article.CreatedDate,
                ModifiedDate = article.ModifiedDate,
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
