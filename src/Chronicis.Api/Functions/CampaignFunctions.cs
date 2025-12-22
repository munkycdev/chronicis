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
    /// Add a member to a campaign
    /// </summary>
    [Function("AddCampaignMember")]
    public async Task<HttpResponseData> AddCampaignMember(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "campaigns/{id:guid}/members")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<CampaignMemberAddDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Email is required" });
            return badRequest;
        }

        _logger.LogInformation("Adding member {Email} to campaign {CampaignId} by user {UserId}", 
            dto.Email, id, user.Id);

        var member = await _campaignService.AddMemberAsync(id, dto, user.Id);

        if (member == null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Unable to add member. User not found, already a member, or you don't have permission." });
            return badRequest;
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(member);
        return response;
    }

    /// <summary>
    /// Update a campaign member's role
    /// </summary>
    [Function("UpdateCampaignMember")]
    public async Task<HttpResponseData> UpdateCampaignMember(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "campaigns/{campaignId:guid}/members/{userId:guid}")] HttpRequestData req,
        Guid campaignId,
        Guid userId,
        FunctionContext context)
    {
        var requestingUser = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<CampaignMemberUpdateDto>(req.Body, _jsonOptions);
        if (dto == null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Invalid request body" });
            return badRequest;
        }

        _logger.LogInformation("Updating member {MemberUserId} in campaign {CampaignId} by user {UserId}", 
            userId, campaignId, requestingUser.Id);

        var member = await _campaignService.UpdateMemberAsync(campaignId, userId, dto, requestingUser.Id);

        if (member == null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Unable to update member. Member not found, cannot demote last DM, or you don't have permission." });
            return badRequest;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(member);
        return response;
    }

    /// <summary>
    /// Remove a member from a campaign
    /// </summary>
    [Function("RemoveCampaignMember")]
    public async Task<HttpResponseData> RemoveCampaignMember(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "campaigns/{campaignId:guid}/members/{userId:guid}")] HttpRequestData req,
        Guid campaignId,
        Guid userId,
        FunctionContext context)
    {
        var requestingUser = context.GetRequiredUser();

        _logger.LogInformation("Removing member {MemberUserId} from campaign {CampaignId} by user {UserId}", 
            userId, campaignId, requestingUser.Id);

        var success = await _campaignService.RemoveMemberAsync(campaignId, userId, requestingUser.Id);

        if (!success)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Unable to remove member. Member not found, cannot remove last DM, or you don't have permission." });
            return badRequest;
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
