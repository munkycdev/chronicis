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
    private readonly ISummaryService _summaryService;
    private readonly ILogger<AISummaryFunctions> _logger;

    public AISummaryFunctions(
        ISummaryService summaryService,
        ILogger<AISummaryFunctions> logger)
    {
        _summaryService = summaryService;
        _logger = logger;
    }

    [Function("GetSummaryEstimate")]
    public async Task<HttpResponseData> GetSummaryEstimate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:guid}/summary/estimate")]
        HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var estimate = await _summaryService.EstimateArticleSummaryAsync(id);

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
            await response.WriteStringAsync("Error: " + ex.Message);
            return response;
        }
    }

    [Function("GenerateSummary")]
    public async Task<HttpResponseData> GenerateSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles/{id:guid}/summary/generate")]
        HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            reader.Dispose();

            GenerateSummaryRequestDto? request = null;
            if (!string.IsNullOrEmpty(requestBody))
            {
                request = JsonSerializer.Deserialize<GenerateSummaryRequestDto>(requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var result = await _summaryService.GenerateArticleSummaryAsync(id, request);

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
                ErrorMessage = "Server error: " + ex.Message
            });
            return response;
        }
    }

    [Function("GetArticleSummary")]
    public async Task<HttpResponseData> GetArticleSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:guid}/summary")]
        HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var summary = await _summaryService.GetArticleSummaryAsync(id);

            if (summary == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Article " + id + " not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(summary);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Error: " + ex.Message);
            return response;
        }
    }

    [Function("ClearArticleSummary")]
    public async Task<HttpResponseData> ClearArticleSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "articles/{id:guid}/summary")]
        HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var success = await _summaryService.ClearArticleSummaryAsync(id);

            if (!success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Article " + id + " not found");
                return notFoundResponse;
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing summary for article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Error: " + ex.Message);
            return response;
        }
    }
}
