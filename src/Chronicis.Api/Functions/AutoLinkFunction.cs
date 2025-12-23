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
/// Azure Function for auto-linking article content.
/// </summary>
public class AutoLinkFunction
{
    private readonly ChronicisDbContext _context;
    private readonly IAutoLinkService _autoLinkService;
    private readonly ILogger<AutoLinkFunction> _logger;

    public AutoLinkFunction(
        ChronicisDbContext context,
        IAutoLinkService autoLinkService,
        ILogger<AutoLinkFunction> logger)
    {
        _context = context;
        _autoLinkService = autoLinkService;
        _logger = logger;
    }

    /// <summary>
    /// Scans article content and returns modified content with wiki links inserted.
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
            // Parse request body
            var request = await req.ReadFromJsonAsync<AutoLinkRequestDto>();
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body is required");
                return badRequest;
            }

            // Verify article exists and user has access
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == articleId && a.CreatedBy == user.Id)
                .Select(a => new { a.Id, a.WorldId })
                .FirstOrDefaultAsync();

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            if (!article.WorldId.HasValue)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Article must belong to a world");
                return badRequest;
            }

            // Run auto-link detection
            var result = await _autoLinkService.FindAndInsertLinksAsync(
                articleId,
                article.WorldId.Value,
                request.Body,
                user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-linking article {ArticleId}", articleId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error processing auto-link: {ex.Message}");
            return errorResponse;
        }
    }
}
