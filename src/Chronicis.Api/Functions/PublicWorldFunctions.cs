using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for anonymous public access to worlds.
/// All endpoints are marked [AllowAnonymous] and do not require authentication.
/// </summary>
[AllowAnonymous]
public class PublicWorldFunctions
{
    private readonly IPublicWorldService _publicWorldService;
    private readonly ILogger<PublicWorldFunctions> _logger;

    public PublicWorldFunctions(IPublicWorldService publicWorldService, ILogger<PublicWorldFunctions> logger)
    {
        _publicWorldService = publicWorldService;
        _logger = logger;
    }

    /// <summary>
    /// Get a public world by its public slug.
    /// No authentication required.
    /// </summary>
    [Function("GetPublicWorld")]
    public async Task<HttpResponseData> GetPublicWorld(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/worlds/{publicSlug}")] HttpRequestData req,
        string publicSlug)
    {
        _logger.LogInformation("Anonymous request for public world '{PublicSlug}'", publicSlug);

        var world = await _publicWorldService.GetPublicWorldAsync(publicSlug);

        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or is not public" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(world);
        return response;
    }

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// No authentication required.
    /// </summary>
    [Function("GetPublicArticleTree")]
    public async Task<HttpResponseData> GetPublicArticleTree(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/worlds/{publicSlug}/articles")] HttpRequestData req,
        string publicSlug)
    {
        _logger.LogInformation("Anonymous request for public article tree in world '{PublicSlug}'", publicSlug);

        var articles = await _publicWorldService.GetPublicArticleTreeAsync(publicSlug);

        // Note: We return empty list if world not found (vs 404) to avoid leaking info about existence
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(articles);
        return response;
    }

    /// <summary>
    /// Get a specific public article by path.
    /// Path format: article-slug/child-slug (does not include world slug)
    /// No authentication required.
    /// </summary>
    [Function("GetPublicArticle")]
    public async Task<HttpResponseData> GetPublicArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/worlds/{publicSlug}/articles/{*articlePath}")] HttpRequestData req,
        string publicSlug,
        string? articlePath)
    {
        // If articlePath is empty/null, this should have been handled by GetPublicArticleTree
        // but Azure Functions catch-all routes can capture empty strings
        if (string.IsNullOrEmpty(articlePath))
        {
            _logger.LogDebug("Empty articlePath, redirecting to GetPublicArticleTree logic");
            var articles = await _publicWorldService.GetPublicArticleTreeAsync(publicSlug);
            var treeResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await treeResponse.WriteAsJsonAsync(articles);
            return treeResponse;
        }

        _logger.LogInformation("Anonymous request for public article '{ArticlePath}' in world '{PublicSlug}'", 
            articlePath, publicSlug);

        var article = await _publicWorldService.GetPublicArticleAsync(publicSlug, articlePath);

        if (article == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Article not found or is not public" });
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(article);
        return response;
    }
}
