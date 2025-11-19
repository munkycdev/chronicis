using Chronicis.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        public async Task<IActionResult> GetRootArticles(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles")] HttpRequest req)
        {
            _logger.LogInformation("GetRootArticles endpoint called");

            try
            {
                var articles = await _articleService.GetRootArticlesAsync();
                return new OkObjectResult(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching root articles");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/articles/{id}
        /// Returns detailed information for a specific article including breadcrumbs.
        /// </summary>
        [Function("GetArticleDetail")]
        public async Task<IActionResult> GetArticleDetail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:int}")] HttpRequest req,
            int id)
        {
            _logger.LogInformation("GetArticleDetail endpoint called for ID: {ArticleId}", id);

            try
            {
                var article = await _articleService.GetArticleDetailAsync(id);
                
                if (article == null)
                {
                    return new NotFoundObjectResult(new { message = $"Article {id} not found" });
                }

                return new OkObjectResult(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching article {ArticleId}", id);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/articles/{id}/children
        /// Returns all child articles of the specified parent article.
        /// </summary>
        [Function("GetArticleChildren")]
        public async Task<IActionResult> GetArticleChildren(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id:int}/children")] HttpRequest req,
            int id)
        {
            _logger.LogInformation("GetArticleChildren endpoint called for parent ID: {ParentId}", id);

            try
            {
                var children = await _articleService.GetChildrenAsync(id);
                return new OkObjectResult(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching children for article {ParentId}", id);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
