using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Function for moving articles in the hierarchy (drag-and-drop reorganization).
/// Authentication is handled globally by AuthenticationMiddleware.
/// </summary>
public class MoveArticle
{
    private readonly IArticleService _articleService;
    private readonly ILogger<MoveArticle> _logger;

    public MoveArticle(IArticleService articleService, ILogger<MoveArticle> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>
    /// PATCH /api/articles/{id}/parent
    /// Moves an article to a new parent (or to root level if newParentId is null).
    /// </summary>
    [Function("MoveArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "articles/{id:guid}/parent")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            // Parse the request body
            var moveDto = await req.ReadFromJsonAsync<ArticleMoveDto>();

            if (moveDto == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Invalid request body" });
                return badRequestResponse;
            }

            // Perform the move
            var (success, errorMessage) = await _articleService.MoveArticleAsync(id, moveDto.NewParentId, user.Id);

            if (!success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { message = errorMessage });
                return errorResponse;
            }

            // Return success with the updated article
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Article moved successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving article {ArticleId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Internal server error");
            return response;
        }
    }
}
