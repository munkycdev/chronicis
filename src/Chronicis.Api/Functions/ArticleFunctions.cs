using Chronicis.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Chronicis.Api.Functions
{
    /// <summary>
    /// Azure Functions HTTP endpoints for Article operations.
    /// Phase 1: Read-only operations (GET endpoints).
    /// </summary>
    public class ArticleFunctions
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticleFunctions> _logger;

        public ArticleFunctions(IArticleService articleService, ILogger<ArticleFunctions> logger)
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles")] HttpRequestData req)
        {
            _logger.LogInformation("GetRootArticles endpoint called");

            try
            {
                var articles = await _articleService.GetRootArticlesAsync();

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
            int id)
        {
            _logger.LogInformation("GetArticleDetail endpoint called for ID: {ArticleId}", id);

            try
            {
                var article = await _articleService.GetArticleDetailAsync(id);

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
            int id)
        {
            _logger.LogInformation("GetArticleChildren endpoint called for parent ID: {ParentId}", id);

            try
            {
                var children = await _articleService.GetChildrenAsync(id);

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
}