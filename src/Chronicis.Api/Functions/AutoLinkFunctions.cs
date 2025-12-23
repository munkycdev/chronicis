using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for auto-linking content in articles.
/// </summary>
public class AutoLinkFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly IAutoLinkService _autoLinkService;
    private readonly ILogger<AutoLinkFunctions> _logger;

    public AutoLinkFunctions(
        ChronicisDbContext context,
        IAutoLinkService autoLinkService,
        ILogger<AutoLinkFunctions> logger)
    {
        _context = context;
        _autoLinkService = autoLinkService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/articles/{articleId}/auto-link
    /// Scans article content for text matching existing article titles
    /// and returns modified content with wiki links inserted.
    /// </summary>
    [Function("AutoLinkArticle")]
    public async Task<HttpResponseData> AutoLinkArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles/{articleId:guid}/auto-link")] HttpRequestData req,
        FunctionContext context,
        Guid articleId)
    {
        var user = context.GetRequiredUser();

        try
        {
            // Get the article to verify ownership and get worldId
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == articleId && a.CreatedBy == user.Id)
                .Select(a => new { a.Id, a.WorldId })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { message = "Article not found" });
                return notFoundResponse;
            }

            if (!article.WorldId.HasValue)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Article has no world assigned" });
                return badRequestResponse;
            }

            // Parse request body
            var requestBody = await req.ReadFromJsonAsync<AutoLinkRequestDto>();
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Body))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Request body is required" });
                return badRequestResponse;
            }

            // Call the auto-link service
            var result = await _autoLinkService.FindAndInsertLinksAsync(
                articleId,
                article.WorldId.Value,
                requestBody.Body,
                user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-linking article {ArticleId}", articleId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { message = "Error processing auto-link request" });
            return errorResponse;
        }
    }
}
