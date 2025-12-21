using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions HTTP endpoints for Article operations.
/// Authentication is handled globally by AuthenticationMiddleware.
/// </summary>
public class ArticleFunctions
{
    private readonly IArticleService _articleService;
    private readonly ILogger<ArticleFunctions> _logger;

    public ArticleFunctions(
        IArticleService articleService,
        ILogger<ArticleFunctions> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/articles
    /// Returns all root-level articles (those without a parent).
    /// </summary>
    [Function("GetRootArticles")]
    public async Task<HttpResponseData> GetRootArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Check for optional worldId query parameter
        Guid? worldId = null;
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        if (Guid.TryParse(query["worldId"], out var parsedWorldId))
        {
            worldId = parsedWorldId;
        }

        try
        {
            var articles = await _articleService.GetRootArticlesAsync(user.Id, worldId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(articles);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching root articles");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// GET /api/articles/all
    /// Returns all articles for the current user in a flat list (no hierarchy).
    /// </summary>
    [Function("GetAllArticles")]
    public async Task<HttpResponseData> GetAllArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/all")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        // Check for optional worldId query parameter
        Guid? worldId = null;
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        if (Guid.TryParse(query["worldId"], out var parsedWorldId))
        {
            worldId = parsedWorldId;
        }

        try
        {
            var articles = await _articleService.GetAllArticlesAsync(user.Id, worldId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(articles);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all articles");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// GET /api/articles/{id}
    /// Returns detailed information for a specific article including breadcrumbs.
    /// </summary>
    [Function("GetArticleDetail")]
    public async Task<HttpResponseData> GetArticleDetail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var article = await _articleService.GetArticleDetailAsync(id, user.Id);

            if (article == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { message = $"Article {id} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(article);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// GET /api/articles/{id}/children
    /// Returns all child articles of the specified parent article.
    /// </summary>
    [Function("GetArticleChildren")]
    public async Task<HttpResponseData> GetArticleChildren(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:guid}/children")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var children = await _articleService.GetChildrenAsync(id, user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(children);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching children for article {ParentId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }

    /// <summary>
    /// GET /api/articles/{id}/hashtags
    /// Gets the hashtags for a specific article
    /// </summary>
    [Function("GetArticleHashtags")]
    public async Task<HttpResponseData> GetArticleHashtags(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:guid}/hashtags")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var hashtags = await _articleService.GetArticleHashtagsAsync(id, user.Id) ?? new List<HashtagDto>();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(hashtags);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hashtags for Article {Id}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error retrieving hashtags: " + ex.Message);
            return errorResponse;
        }
    }
}
