using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class HashtagFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<HashtagFunctions> _logger;

    public HashtagFunctions(
        ChronicisDbContext context,
        ILogger<HashtagFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("GetAllHashtags")]
    public async Task<HttpResponseData> GetAllHashtags(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags")]
        HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

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
            _logger.LogError(ex, "Error retrieving hashtags");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving hashtags: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetHashtagByName")]
    public async Task<HttpResponseData> GetHashtagByName(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags/{name}")]
        HttpRequestData req,
        FunctionContext context,
        string name)
    {
        var user = context.GetRequiredUser();

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
            _logger.LogError(ex, "Error retrieving hashtag {Name}", name);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving hashtag: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("LinkHashtag")]
    public async Task<HttpResponseData> LinkHashtag(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hashtags/{name}/link")]
        HttpRequestData req,
        FunctionContext context,
        string name)
    {
        var user = context.GetRequiredUser();

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

            var hashtag = await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Name == hashtagName);

            if (hashtag == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Hashtag '{name}' not found");
                return notFound;
            }

            var articleExists = await _context.Articles
                .AnyAsync(a => a.Id == linkDto.ArticleId && a.UserId == user.Id);

            if (!articleExists)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Article with ID {linkDto.ArticleId} not found");
                return notFound;
            }

            hashtag.LinkedArticleId = linkDto.ArticleId;
            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Hashtag '{name}' linked to article {linkDto.ArticleId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking hashtag {Name}", name);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error linking hashtag: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetHashtagPreview")]
    public async Task<HttpResponseData> GetHashtagPreview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hashtags/{name}/preview")]
        HttpRequestData req,
        FunctionContext context,
        string name)
    {
        var user = context.GetRequiredUser();
        var response = req.CreateResponse();

        try
        {
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

            var previewText = hashtag.LinkedArticle.Body?.Length > 200
                ? string.Concat(hashtag.LinkedArticle.Body.AsSpan(0, 200), "...")
                : hashtag.LinkedArticle.Body;

            var preview = new HashtagPreviewDto
            {
                HasArticle = true,
                HashtagName = hashtag.Name,
                ArticleId = hashtag.LinkedArticle.Id,
                ArticleTitle = hashtag.LinkedArticle.Title,
                ArticleSlug = SlugGenerator.GenerateSlug(hashtag.LinkedArticle.Title),
                PreviewText = previewText,
                LastModified = hashtag.LinkedArticle.ModifiedDate ?? hashtag.LinkedArticle.CreatedDate
            };

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hashtag preview for {Name}", name);
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ex.Message });
        }

        return response;
    }
}
