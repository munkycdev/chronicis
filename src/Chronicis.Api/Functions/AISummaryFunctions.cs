using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class AISummaryFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly IAISummaryService _summaryService;
    private readonly ILogger<AISummaryFunctions> _logger;

    public AISummaryFunctions(
        ChronicisDbContext context,
        IAISummaryService summaryService,
        ILogger<AISummaryFunctions> logger)
    {
        _context = context;
        _summaryService = summaryService;
        _logger = logger;
    }

    [Function("GetSummaryEstimate")]
    public async Task<HttpResponseData> GetSummaryEstimate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id}/summary/estimate")]
        HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting summary estimate for article {ArticleId}, user {UserId}", id, user.Id);

        try
        {
            var estimate = await _summaryService.EstimateCostAsync(id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(estimate);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Article {ArticleId} not found", id);
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteStringAsync(ex.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary estimate for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GenerateSummary")]
    public async Task<HttpResponseData> GenerateSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles/{id}/summary/generate")]
        HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Generating AI summary for article {ArticleId}, user {UserId}", id, user.Id);

        try
        {
            var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            reader.Dispose();

            var request = string.IsNullOrEmpty(requestBody)
                ? new GenerateSummaryRequestDto { ArticleId = id }
                : JsonSerializer.Deserialize<GenerateSummaryRequestDto>(requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? new GenerateSummaryRequestDto { ArticleId = id };

            var result = await _summaryService.GenerateSummaryAsync(id, request.MaxOutputTokens);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new SummaryGenerationDto
            {
                Success = false,
                ErrorMessage = $"Server error: {ex.Message}"
            });
            return response;
        }
    }

    [Function("GetArticleSummary")]
    public async Task<HttpResponseData> GetArticleSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id}/summary")]
        HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting summary for article {ArticleId}, user {UserId}", id, user.Id);

        try
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == id && a.UserId == user.Id)
                .Select(a => new ArticleSummaryDto
                {
                    ArticleId = a.Id,
                    Summary = a.AISummary,
                    GeneratedDate = a.AISummaryGeneratedDate
                })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Article {id} not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(article);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("ClearArticleSummary")]
    public async Task<HttpResponseData> ClearArticleSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "articles/{id}/summary")]
        HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Clearing summary for article {ArticleId}, user {UserId}", id, user.Id);

        try
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (article == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Article {id} not found");
                return notFoundResponse;
            }

            article.AISummary = null;
            article.AISummaryGeneratedDate = null;
            await _context.SaveChangesAsync();

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing summary for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
