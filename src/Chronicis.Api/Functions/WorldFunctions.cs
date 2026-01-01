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
/// Azure Functions for World management
/// </summary>
public class WorldFunctions
{
    private readonly IWorldService _worldService;
    private readonly ILogger<WorldFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorldFunctions(IWorldService worldService, ILogger<WorldFunctions> logger)
    {
        _worldService = worldService;
        _logger = logger;
    }

    /// <summary>
    /// Get all worlds the user has access to
    /// </summary>
    [Function("GetWorlds")]
    public async Task<HttpResponseData> GetWorlds(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting worlds for user {UserId}", user.Id);

        var worlds = await _worldService.GetUserWorldsAsync(user.Id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(worlds);
        return response;
    }

    /// <summary>
    /// Get a specific world with its campaigns
    /// </summary>
    [Function("GetWorld")]
    public async Task<HttpResponseData> GetWorld(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{id:guid}")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting world {WorldId} for user {UserId}", id, user.Id);

        var world = await _worldService.GetWorldAsync(id, user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(world);
        return response;
    }

    /// <summary>
    /// Create a new world
    /// </summary>
    [Function("CreateWorld")]
    public async Task<HttpResponseData> CreateWorld(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "worlds")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<WorldCreateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Name is required" });
            return badRequest;
        }

        _logger.LogInformation("Creating world '{Name}' for user {UserId}", dto.Name, user.Id);

        var world = await _worldService.CreateWorldAsync(dto, user.Id);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(world);
        return response;
    }

    /// <summary>
    /// Check if a public slug is available
    /// </summary>
    [Function("CheckPublicSlug")]
    public async Task<HttpResponseData> CheckPublicSlug(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "worlds/{id:guid}/check-public-slug")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Verify user owns this world
        var world = await _worldService.GetWorldAsync(id, user.Id);
        if (world == null || world.OwnerId != user.Id)
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = "Only the world owner can check public slugs" });
            return forbidden;
        }

        var dto = await JsonSerializer.DeserializeAsync<PublicSlugCheckDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Slug))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Slug is required" });
            return badRequest;
        }

        _logger.LogInformation("Checking public slug '{Slug}' for world {WorldId}", dto.Slug, id);

        var result = await _worldService.CheckPublicSlugAsync(dto.Slug, id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    /// <summary>
    /// Update a world
    /// </summary>
    [Function("UpdateWorld")]
    public async Task<HttpResponseData> UpdateWorld(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "worlds/{id:guid}")] HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var dto = await JsonSerializer.DeserializeAsync<WorldUpdateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Name is required" });
            return badRequest;
        }

        _logger.LogInformation("Updating world {WorldId} for user {UserId}", id, user.Id);

        var world = await _worldService.UpdateWorldAsync(id, dto, user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(world);
        return response;
    }
}
