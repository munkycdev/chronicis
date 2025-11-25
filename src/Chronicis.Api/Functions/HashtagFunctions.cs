using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for hashtag operations
/// </summary>
public class HashtagFunctions
{
    private readonly ChronicisDbContext _context;

    public HashtagFunctions(ChronicisDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/hashtags - Get all hashtags with usage counts
    /// </summary>
    [Function("GetAllHashtags")]
    public async Task<HttpResponseData> GetAllHashtags(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags")] 
        HttpRequestData req)
    {
        try
        {
            var hashtags = await _context.Hashtags
                .Include(h => h.LinkedArticle)
                .Include(h => h.ArticleHashtags)
                .Select(h => new HashtagDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    LinkedArticleId = h.LinkedArticleId,
                    LinkedArticleTitle = h.LinkedArticle != null ? h.LinkedArticle.Title : null,
                    UsageCount = h.ArticleHashtags.Count,
                    CreatedDate = h.CreatedDate
                })
                .OrderByDescending(h => h.UsageCount)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(hashtags);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving hashtags: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// GET /api/hashtags/{name} - Get specific hashtag by name
    /// </summary>
    [Function("GetHashtagByName")]
    public async Task<HttpResponseData> GetHashtagByName(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags/{name}")] 
        HttpRequestData req,
        string name)
    {
        try
        {
            var hashtagName = name.ToLowerInvariant();

            var hashtag = await _context.Hashtags
                .Include(h => h.LinkedArticle)
                .Include(h => h.ArticleHashtags)
                .Where(h => h.Name == hashtagName)
                .Select(h => new HashtagDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    LinkedArticleId = h.LinkedArticleId,
                    LinkedArticleTitle = h.LinkedArticle != null ? h.LinkedArticle.Title : null,
                    UsageCount = h.ArticleHashtags.Count,
                    CreatedDate = h.CreatedDate
                })
                .FirstOrDefaultAsync();

            if (hashtag == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Hashtag '{name}' not found");
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(hashtag);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving hashtag: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/hashtags/{name}/link - Link a hashtag to an article
    /// </summary>
    [Function("LinkHashtag")]
    public async Task<HttpResponseData> LinkHashtag(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hashtags/{name}/link")] 
        HttpRequestData req,
        string name)
    {
        try
        {
            var linkDto = await JsonSerializer.DeserializeAsync<LinkHashtagDto>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (linkDto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            var hashtagName = name.ToLowerInvariant();

            // Find hashtag
            var hashtag = await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Name == hashtagName);

            if (hashtag == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Hashtag '{name}' not found");
                return notFound;
            }

            // Verify article exists
            var articleExists = await _context.Articles
                .AnyAsync(a => a.Id == linkDto.ArticleId);

            if (!articleExists)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Article with ID {linkDto.ArticleId} not found");
                return notFound;
            }

            // Update the link
            hashtag.LinkedArticleId = linkDto.ArticleId;
            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Hashtag '{name}' linked to article {linkDto.ArticleId}");
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error linking hashtag: {ex.Message}");
            return errorResponse;
        }
    }
}
