using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class LinkResolutionFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<LinkResolutionFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LinkResolutionFunctions(
        ChronicisDbContext context,
        ILogger<LinkResolutionFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("ResolveLinks")]
    public async Task<HttpResponseData> ResolveLinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles/resolve-links")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Parse request body
            var request = await JsonSerializer.DeserializeAsync<LinkResolutionRequestDto>(req.Body, _jsonOptions);

            if (request == null || !request.ArticleIds.Any())
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request must include at least one article ID");
                return badRequest;
            }

            // Query all requested article IDs in a single query (no N+1!)
            var existingArticles = await _context.Articles
                .Where(a => request.ArticleIds.Contains(a.Id) && a.CreatedBy == user.Id)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Slug
                })
                .ToListAsync();

            // Build response dictionary
            var result = new Dictionary<Guid, ResolvedLinkDto>();

            foreach (var articleId in request.ArticleIds)
            {
                var article = existingArticles.FirstOrDefault(a => a.Id == articleId);

                if (article != null)
                {
                    // Article exists
                    result[articleId] = new ResolvedLinkDto
                    {
                        ArticleId = articleId,
                        Exists = true,
                        Title = article.Title,
                        Slug = article.Slug
                    };
                }
                else
                {
                    // Article doesn't exist - broken link!
                    result[articleId] = new ResolvedLinkDto
                    {
                        ArticleId = articleId,
                        Exists = false,
                        Title = null,
                        Slug = null
                    };

                    // Log warning for broken link
                    _logger.LogWarning(
                        "Broken link detected: Article {ArticleId} does not exist",
                        articleId);
                }
            }

            stopwatch.Stop();

            // Log metrics
            _logger.LogInformation(
                "Resolved {LinkCount} links in {DurationMs}ms",
                request.ArticleIds.Count,
                stopwatch.ElapsedMilliseconds);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new LinkResolutionResponseDto
            {
                Articles = result
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving links");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error resolving links: {ex.Message}");
            return errorResponse;
        }
    }
}
