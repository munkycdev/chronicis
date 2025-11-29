using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for hashtag operations
/// </summary>
public class HashtagFunctions : BaseAuthenticatedFunction
{
    private readonly ChronicisDbContext _context;

    public HashtagFunctions(ChronicisDbContext context,
            ILogger<HashtagFunctions> logger,
            IUserService userService,
            IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger)
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
            var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
            if (authErrorResponse != null) return authErrorResponse;

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
            var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
            if (authErrorResponse != null) return authErrorResponse;

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
            var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
            if (authErrorResponse != null) return authErrorResponse;

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

    [Function("GetHashtagPreview")]
    public async Task<HttpResponseData> GetHashtagPreview(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags/{name}/preview")]
    HttpRequestData req,
    string name)
    {
        var response = req.CreateResponse();

        try
        {
            var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
            if (authErrorResponse != null) return authErrorResponse;

            var normalizedName = name.ToLowerInvariant();

            var hashtag = await _context.Hashtags
                .Include(h => h.LinkedArticle)
                .FirstOrDefaultAsync(h => h.Name == normalizedName);

            if (hashtag == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new { error = "Hashtag not found" });
                return response;
            }

            if (hashtag.LinkedArticleId == null || hashtag.LinkedArticle == null)
            {
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new
                {
                    hasArticle = false,
                    hashtagName = hashtag.Name
                });
                return response;
            }

            // Get first 200 characters of body for preview
            var previewText = hashtag.LinkedArticle.Body?.Length > 200
                ? string.Concat(hashtag.LinkedArticle.Body.AsSpan(0, 200), "...")
                : hashtag.LinkedArticle.Body;

            var preview = new HashtagPreviewDto
            {
                HasArticle = true,
                HashtagName = hashtag.Name,
                ArticleId = hashtag.LinkedArticle.Id,
                ArticleTitle = hashtag.LinkedArticle.Title,
                ArticleSlug = CreateSlug(hashtag.LinkedArticle.Title),
                PreviewText = previewText,
                LastModified = hashtag.LinkedArticle.ModifiedDate ?? hashtag.LinkedArticle.CreatedDate
            };

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(preview);
        }
        catch (Exception ex)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ex.Message });
        }

        return response;
    }

    private static string CreateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "untitled";

        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(":", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "");
    }

}
