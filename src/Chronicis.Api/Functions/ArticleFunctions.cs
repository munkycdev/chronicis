using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
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
        _logger.LogInformation("GetRootArticles called for user {UserId}", user.Id);

        try
        {
            var articles = await _articleService.GetRootArticlesAsync(user.Id);

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
    /// GET /api/articles/{id}
    /// Returns detailed information for a specific article including breadcrumbs.
    /// </summary>
    [Function("GetArticleDetail")]
    public async Task<HttpResponseData> GetArticleDetail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:int}")] HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("GetArticleDetail called for ID: {ArticleId}, user {UserId}", id, user.Id);

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:int}/children")] HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("GetArticleChildren called for parent ID: {ParentId}, user {UserId}", id, user.Id);

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
}
