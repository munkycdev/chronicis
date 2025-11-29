using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

public class AISummaryFunctions : BaseAuthenticatedFunction
{
    private readonly ChronicisDbContext _context;
    private readonly IAISummaryService _summaryService;

    public AISummaryFunctions(
        ChronicisDbContext context,
        IAISummaryService summaryService,
        ILogger<AISummaryFunctions> logger,
        IUserService userService,
        IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger)
    {
        _context = context;
        _summaryService = summaryService;
    }

    [Function("GetSummaryEstimate")]
    public async Task<HttpResponseData> GetSummaryEstimate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id}/summary/estimate")] 
        HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Getting summary estimate for article {ArticleId}", id);
        
        var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
        if (authErrorResponse != null) return authErrorResponse;

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
        int id)
    {
        _logger.LogInformation("Generating AI summary for article {ArticleId}", id);

        var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
        if (authErrorResponse != null) return authErrorResponse;

        try
        {
            // Parse request body (optional maxTokens)
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
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
        int id)
    {
        _logger.LogInformation("Getting summary for article {ArticleId}", id);

        var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
        if (authErrorResponse != null) return authErrorResponse;

        try
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == id)
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
        int id)
    {
        _logger.LogInformation("Clearing summary for article {ArticleId}", id);

        var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
        if (authErrorResponse != null) return authErrorResponse;

        try
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Article {id} not found");
                return notFoundResponse;
            }

            article.AISummary = null;
            article.AISummaryGeneratedDate = null;
            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
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
