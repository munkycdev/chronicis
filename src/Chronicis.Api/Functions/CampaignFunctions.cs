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
/// Azure Functions for Campaign management
/// </summary>
public class CampaignFunctions
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<CampaignFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CampaignFunctions(ICampaignService campaignService, ILogger<CampaignFunctions> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific campaign with its members
    /// </summary>
    [Function("GetCampaign")]
    public async Task<HttpResponseData> GetCampaign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "campaigns/{id:guid}")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting campaign {CampaignId} for user {UserId}", id, user.Id);

        var campaign = await _campaignService.GetCampaignAsync(id, user.Id);

        if (campaign == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Campaign not found or access denied" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(campaign);
        return response;
    }

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [Function("CreateCampaign")]
    public async Task<HttpResponseData> CreateCampaign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "campaigns")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<CampaignCreateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Name is required" });
            return badRequest;
        }

        if (dto.WorldId == Guid.Empty)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "WorldId is required" });
            return badRequest;
        }

        _logger.LogInformation("Creating campaign '{Name}' in world {WorldId} for user {UserId}", 
            dto.Name, dto.WorldId, user.Id);

        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(dto, user.Id);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(campaign);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = ex.Message });
            return badRequest;
        }
    }

    /// <summary>
    /// Update a campaign
    /// </summary>
    [Function("UpdateCampaign")]
    public async Task<HttpResponseData> UpdateCampaign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "campaigns/{id:guid}")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<CampaignUpdateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Name is required" });
            return badRequest;
        }

        _logger.LogInformation("Updating campaign {CampaignId} for user {UserId}", id, user.Id);

        var campaign = await _campaignService.UpdateCampaignAsync(id, dto, user.Id);

        if (campaign == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Campaign not found or access denied" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(campaign);
        return response;
    }

    /// <summary>
    /// Activate a campaign (makes it the active campaign for quick session creation)
    /// </summary>
    [Function("ActivateCampaign")]
    public async Task<HttpResponseData> ActivateCampaign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "campaigns/{id:guid}/activate")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        _logger.LogInformation("Activating campaign {CampaignId} for user {UserId}", id, user.Id);

        var success = await _campaignService.ActivateCampaignAsync(id, user.Id);

        if (!success)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Unable to activate campaign. Campaign not found or you don't have permission." });
            return badRequest;
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Get the active context (campaign/arc) for a world
    /// </summary>
    [Function("GetActiveContext")]
    public async Task<HttpResponseData> GetActiveContext(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/active-context")] HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        _logger.LogInformation("Getting active context for world {WorldId} for user {UserId}", worldId, user.Id);

        var activeContext = await _campaignService.GetActiveContextAsync(worldId, user.Id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(activeContext);
        return response;
    }
}
