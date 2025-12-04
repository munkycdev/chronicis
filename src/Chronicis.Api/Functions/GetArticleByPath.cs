using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Function for retrieving articles by their hierarchical path.
/// </summary>
public class GetArticleByPath
{
    private readonly IArticleService _articleService;
    private readonly ILogger<GetArticleByPath> _logger;

    public GetArticleByPath(
        IArticleService articleService,
        ILogger<GetArticleByPath> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/articles/by-path/{*path} - Gets an article by its hierarchical slug path.
    /// Example: /api/articles/by-path/sword-coast/waterdeep/castle-ward
    /// </summary>
    [Function("GetArticleByPath")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/by-path/{*path}")] HttpRequestData req,
        FunctionContext context,
        string path)
    {
        var user = context.GetRequiredUser();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Path parameter is required");
                return badRequest;
            }

            _logger.LogInformation("Looking up article by path: {Path} for user {UserId}", path, user.Id);

            var article = await _articleService.GetArticleByPathAsync(path, user.Id);

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Article not found at path: {path}");
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(article);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving article by path: {Path}", path);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving article: {ex.Message}");
            return errorResponse;
        }
    }
}
