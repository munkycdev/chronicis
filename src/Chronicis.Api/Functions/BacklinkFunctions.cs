using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class BacklinkFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<BacklinkFunctions> _logger;

    public BacklinkFunctions(
        ChronicisDbContext context,
        ILogger<BacklinkFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("GetBacklinks")]
    public async Task<HttpResponseData> GetBacklinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{articleId:guid}/backlinks")] HttpRequestData req,
        FunctionContext context,
        Guid articleId)
    {
        var user = context.GetRequiredUser();

        try
        {
            // Query ArticleLinks where TargetArticleId matches
            // Include SourceArticle navigation property to get article details
            // Get distinct source articles (an article might link to the same target multiple times)
            var backlinks = await _context.ArticleLinks
                .Where(al => al.TargetArticleId == articleId)
                .Include(al => al.SourceArticle)
                .Where(al => al.SourceArticle.CreatedBy == user.Id) // User scoping
                .Select(al => new BacklinkDto
                {
                    ArticleId = al.SourceArticle.Id,
                    Title = al.SourceArticle.Title,
                    DisplayPath = al.SourceArticle.Title, // TODO: Build full path in future iteration
                    Slug = al.SourceArticle.Slug,
                    Snippet = null // TODO: Extract snippet with context in future iteration
                })
                .Distinct()
                .OrderBy(b => b.Title)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new BacklinksResponseDto
            {
                Backlinks = backlinks
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backlinks for article {ArticleId}", articleId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting backlinks: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetOutgoingLinks")]
    public async Task<HttpResponseData> GetOutgoingLinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{articleId:guid}/outgoing-links")] HttpRequestData req,
        FunctionContext context,
        Guid articleId)
    {
        var user = context.GetRequiredUser();

        try
        {
            // Query ArticleLinks where SourceArticleId matches (outgoing links)
            // Include TargetArticle navigation property to get article details
            // Get distinct target articles (an article might link to the same target multiple times)
            var outgoingLinks = await _context.ArticleLinks
                .Where(al => al.SourceArticleId == articleId)
                .Include(al => al.TargetArticle)
                .Where(al => al.TargetArticle.CreatedBy == user.Id) // User scoping
                .Select(al => new BacklinkDto
                {
                    ArticleId = al.TargetArticle.Id,
                    Title = al.TargetArticle.Title,
                    DisplayPath = al.TargetArticle.Title, // TODO: Build full path in future iteration
                    Slug = al.TargetArticle.Slug,
                    Snippet = null
                })
                .Distinct()
                .OrderBy(b => b.Title)
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new BacklinksResponseDto
            {
                Backlinks = outgoingLinks
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outgoing links for article {ArticleId}", articleId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting outgoing links: {ex.Message}");
            return errorResponse;
        }
    }
}
