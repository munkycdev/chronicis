using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for World Link management (external resources)
/// </summary>
public class WorldLinkFunctions
{
    private readonly ChronicisDbContext _db;
    private readonly ILogger<WorldLinkFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorldLinkFunctions(ChronicisDbContext db, ILogger<WorldLinkFunctions> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all links for a world (sorted alphabetically by title)
    /// </summary>
    [Function("GetWorldLinks")]
    public async Task<HttpResponseData> GetWorldLinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/links")] HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("Getting links for world {WorldId} by user {UserId}", worldId, user.Id);

        // Verify user has access to the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var links = await _db.WorldLinks
            .AsNoTracking()
            .Where(wl => wl.WorldId == worldId)
            .OrderBy(wl => wl.Title)
            .Select(wl => new WorldLinkDto
            {
                Id = wl.Id,
                WorldId = wl.WorldId,
                Url = wl.Url,
                Title = wl.Title,
                Description = wl.Description,
                CreatedAt = wl.CreatedAt
            })
            .ToListAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(links);
        return response;
    }

    /// <summary>
    /// Create a new link for a world (owner only)
    /// </summary>
    [Function("CreateWorldLink")]
    public async Task<HttpResponseData> CreateWorldLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "worlds/{worldId:guid}/links")] HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Verify user owns the world
        var world = await _db.Worlds
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var dto = await JsonSerializer.DeserializeAsync<WorldLinkCreateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Url) || string.IsNullOrWhiteSpace(dto.Title))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "URL and Title are required" });
            return badRequest;
        }

        // Validate URL format
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Invalid URL format. Must be a valid http or https URL." });
            return badRequest;
        }

        _logger.LogInformation("Creating link '{Title}' for world {WorldId} by user {UserId}", 
            dto.Title, worldId, user.Id);

        var link = new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Url = dto.Url.Trim(),
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.WorldLinks.Add(link);
        await _db.SaveChangesAsync();

        var result = new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        };

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    /// <summary>
    /// Update an existing world link (owner only)
    /// </summary>
    [Function("UpdateWorldLink")]
    public async Task<HttpResponseData> UpdateWorldLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "worlds/{worldId:guid}/links/{linkId:guid}")] HttpRequestData req,
        Guid worldId,
        Guid linkId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Verify user owns the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var link = await _db.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Link not found" });
            return notFound;
        }

        var dto = await JsonSerializer.DeserializeAsync<WorldLinkUpdateDto>(req.Body, _jsonOptions);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Url) || string.IsNullOrWhiteSpace(dto.Title))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "URL and Title are required" });
            return badRequest;
        }

        // Validate URL format
        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Invalid URL format. Must be a valid http or https URL." });
            return badRequest;
        }

        _logger.LogInformation("Updating link {LinkId} for world {WorldId} by user {UserId}", 
            linkId, worldId, user.Id);

        link.Url = dto.Url.Trim();
        link.Title = dto.Title.Trim();
        link.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _db.SaveChangesAsync();

        var result = new WorldLinkDto
        {
            Id = link.Id,
            WorldId = link.WorldId,
            Url = link.Url,
            Title = link.Title,
            Description = link.Description,
            CreatedAt = link.CreatedAt
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    /// <summary>
    /// Delete a world link (owner only)
    /// </summary>
    [Function("DeleteWorldLink")]
    public async Task<HttpResponseData> DeleteWorldLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "worlds/{worldId:guid}/links/{linkId:guid}")] HttpRequestData req,
        Guid worldId,
        Guid linkId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Verify user owns the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == user.Id);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        var link = await _db.WorldLinks
            .FirstOrDefaultAsync(wl => wl.Id == linkId && wl.WorldId == worldId);

        if (link == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Link not found" });
            return notFound;
        }

        _logger.LogInformation("Deleting link {LinkId} for world {WorldId} by user {UserId}", 
            linkId, worldId, user.Id);

        _db.WorldLinks.Remove(link);
        await _db.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
