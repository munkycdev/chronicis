using System.Net;
using System.Text.Json;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Unified summary API endpoints supporting Articles, Campaigns, and Arcs
/// </summary>
public class SummaryFunctions
{
    private readonly ISummaryService _summaryService;
    private readonly ILogger<SummaryFunctions> _logger;

    public SummaryFunctions(ISummaryService summaryService, ILogger<SummaryFunctions> logger)
    {
        _summaryService = summaryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available summary templates
    /// </summary>
    [Function("GetSummaryTemplates")]
    public async Task<HttpResponseData> GetTemplates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "summary/templates")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var templates = await _summaryService.GetTemplatesAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(templates);
        return response;
    }

    #region Campaign Summary Endpoints

    [Function("GetCampaignSummary")]
    public async Task<HttpResponseData> GetCampaignSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "campaigns/{campaignId:guid}/summary")] HttpRequestData req,
        Guid campaignId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var summary = await _summaryService.GetCampaignSummaryAsync(campaignId);
        
        if (summary == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Campaign {campaignId} not found");
            return notFound;
        }
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(summary);
        return response;
    }

    [Function("EstimateCampaignSummaryCost")]
    public async Task<HttpResponseData> EstimateCampaignSummaryCost(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "campaigns/{campaignId:guid}/summary/estimate")] HttpRequestData req,
        Guid campaignId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        try
        {
            var estimate = await _summaryService.EstimateCampaignSummaryAsync(campaignId);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(estimate);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync(ex.Message);
            return notFound;
        }
    }

    [Function("GenerateCampaignSummary")]
    public async Task<HttpResponseData> GenerateCampaignSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "campaigns/{campaignId:guid}/summary/generate")] HttpRequestData req,
        Guid campaignId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        try
        {
            var options = await ParseRequestBody<GenerateSummaryRequestDto>(req);
            var result = await _summaryService.GenerateCampaignSummaryAsync(campaignId, options);
            
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for Campaign {CampaignId}", campaignId);
            return await CreateErrorResponse(req, ex.Message);
        }
    }

    [Function("ClearCampaignSummary")]
    public async Task<HttpResponseData> ClearCampaignSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "campaigns/{campaignId:guid}/summary")] HttpRequestData req,
        Guid campaignId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var success = await _summaryService.ClearCampaignSummaryAsync(campaignId);
        
        if (!success)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Campaign {campaignId} not found");
            return notFound;
        }
        
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    #endregion

    #region Arc Summary Endpoints

    [Function("GetArcSummary")]
    public async Task<HttpResponseData> GetArcSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "arcs/{arcId:guid}/summary")] HttpRequestData req,
        Guid arcId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var summary = await _summaryService.GetArcSummaryAsync(arcId);
        
        if (summary == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Arc {arcId} not found");
            return notFound;
        }
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(summary);
        return response;
    }

    [Function("EstimateArcSummaryCost")]
    public async Task<HttpResponseData> EstimateArcSummaryCost(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "arcs/{arcId:guid}/summary/estimate")] HttpRequestData req,
        Guid arcId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        try
        {
            var estimate = await _summaryService.EstimateArcSummaryAsync(arcId);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(estimate);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync(ex.Message);
            return notFound;
        }
    }

    [Function("GenerateArcSummary")]
    public async Task<HttpResponseData> GenerateArcSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "arcs/{arcId:guid}/summary/generate")] HttpRequestData req,
        Guid arcId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        try
        {
            var options = await ParseRequestBody<GenerateSummaryRequestDto>(req);
            var result = await _summaryService.GenerateArcSummaryAsync(arcId, options);
            
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for Arc {ArcId}", arcId);
            return await CreateErrorResponse(req, ex.Message);
        }
    }

    [Function("ClearArcSummary")]
    public async Task<HttpResponseData> ClearArcSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "arcs/{arcId:guid}/summary")] HttpRequestData req,
        Guid arcId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var success = await _summaryService.ClearArcSummaryAsync(arcId);
        
        if (!success)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Arc {arcId} not found");
            return notFound;
        }
        
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    #endregion

    #region Helpers

    private static async Task<T?> ParseRequestBody<T>(HttpRequestData req) where T : class
    {
        var body = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(body))
            return null;
            
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message)
    {
        var error = req.CreateResponse(HttpStatusCode.InternalServerError);
        await error.WriteAsJsonAsync(new SummaryGenerationDto
        {
            Success = false,
            ErrorMessage = message
        });
        return error;
    }

    #endregion
}
