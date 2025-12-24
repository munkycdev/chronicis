using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions HTTP endpoints for Arc operations.
/// </summary>
public class ArcFunctions
{
    private readonly IArcService _arcService;
    private readonly ILogger<ArcFunctions> _logger;

    public ArcFunctions(IArcService arcService, ILogger<ArcFunctions> logger)
    {
        _arcService = arcService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/campaigns/{campaignId}/arcs
    /// Returns all arcs for a campaign.
    /// </summary>
    [Function("GetArcsByCampaign")]
    public async Task<HttpResponseData> GetArcsByCampaign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "campaigns/{campaignId:guid}/arcs")] HttpRequestData req,
        FunctionContext context,
        Guid campaignId)
    {
        var user = context.GetRequiredUser();

        try
        {
            var arcs = await _arcService.GetArcsByCampaignAsync(campaignId, user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(arcs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching arcs for campaign {CampaignId}", campaignId);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// GET /api/arcs/{id}
    /// Returns details for a specific arc.
    /// </summary>
    [Function("GetArc")]
    public async Task<HttpResponseData> GetArc(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "arcs/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var arc = await _arcService.GetArcAsync(id, user.Id);

            if (arc == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { message = $"Arc {id} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(arc);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching arc {ArcId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// POST /api/arcs
    /// Creates a new arc.
    /// </summary>
    [Function("CreateArc")]
    public async Task<HttpResponseData> CreateArc(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "arcs")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        try
        {
            var dto = await req.ReadFromJsonAsync<ArcCreateDto>();
            if (dto == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Invalid request body" });
                return badRequestResponse;
            }

            var arc = await _arcService.CreateArcAsync(dto, user.Id);

            if (arc == null)
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                await forbiddenResponse.WriteAsJsonAsync(new { message = "Cannot create arc in this campaign" });
                return forbiddenResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(arc);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating arc");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// PUT /api/arcs/{id}
    /// Updates an existing arc.
    /// </summary>
    [Function("UpdateArc")]
    public async Task<HttpResponseData> UpdateArc(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "arcs/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var dto = await req.ReadFromJsonAsync<ArcUpdateDto>();
            if (dto == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Invalid request body" });
                return badRequestResponse;
            }

            var arc = await _arcService.UpdateArcAsync(id, dto, user.Id);

            if (arc == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { message = $"Arc {id} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(arc);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating arc {ArcId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// DELETE /api/arcs/{id}
    /// Deletes an arc (only if empty).
    /// </summary>
    [Function("DeleteArc")]
    public async Task<HttpResponseData> DeleteArc(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "arcs/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var success = await _arcService.DeleteArcAsync(id, user.Id);

            if (!success)
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new { message = "Cannot delete arc - it may have sessions or not exist" });
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting arc {ArcId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }
}
