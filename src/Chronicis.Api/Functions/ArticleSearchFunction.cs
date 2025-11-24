using Chronicis.Api.Data;
using Chronicis.Shared.DTOs; 
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Chronicis.Api.Functions;

public class ArticleSearchFunction
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ArticleSearchFunction> _logger;

    public ArticleSearchFunction(ChronicisDbContext context, ILogger<ArticleSearchFunction> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("SearchArticles")]
    public async Task<HttpResponseData> SearchArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/search")] HttpRequestData req)
    {
        _logger.LogInformation("SearchArticles endpoint called");

        try
        {
            var queryString = HttpUtility.ParseQueryString(req.Url.Query);
            var query = queryString["query"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                var emptyResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new List<ArticleSearchResultDto>());
                return emptyResponse;
            }

            _logger.LogInformation("Searching articles with query: {Query}", query);

            // Search for articles with titles OR body containing the query
            var matchingArticles = await _context.Articles
                .Where(a => EF.Functions.Like(a.Title, $"%{query}%") ||
                           EF.Functions.Like(a.Body, $"%{query}%"))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Body,
                    a.ParentId,
                    a.CreatedDate,
                    a.EffectiveDate  // Use new field
                })
                .ToListAsync();

            var results = new List<ArticleSearchResultDto>();

            foreach (var article in matchingArticles)
            {
                var ancestorPath = await BuildAncestorPath(article.Id);

                // Create snippet showing where match was found
                var matchSnippet = GetMatchSnippet(article.Title, article.Body, query);

                results.Add(new ArticleSearchResultDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    Body = article.Body,
                    MatchSnippet = matchSnippet,
                    AncestorPath = ancestorPath,  // Changed from AncestorDto to BreadcrumbDto
                    CreatedDate = article.CreatedDate,
                    EffectiveDate = article.EffectiveDate
                });
            }

            _logger.LogInformation("Found {Count} matching articles", results.Count);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching articles");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            return errorResponse;
        }
    }

    private async Task<List<BreadcrumbDto>> BuildAncestorPath(int articleId)  // Changed return type
    {
        var ancestors = new List<BreadcrumbDto>();  // Changed type
        var currentId = articleId;

        while (true)
        {
            var article = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => new { a.Id, a.Title, a.ParentId })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            ancestors.Insert(0, new BreadcrumbDto  // Changed type
            {
                Id = article.Id,
                Title = article.Title
            });

            if (article.ParentId == null)
                break;

            currentId = article.ParentId.Value;
        }

        return ancestors;
    }

    private string GetMatchSnippet(string title, string? body, string query)
    {
        // Check title first
        if (title.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return $"Title: {title}";
        }

        // Then check body
        if (!string.IsNullOrEmpty(body) && body.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            var index = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            var start = Math.Max(0, index - 50);
            var length = Math.Min(100, body.Length - start);
            var snippet = body.Substring(start, length);

            return start > 0 ? $"...{snippet}..." : $"{snippet}...";
        }

        return string.Empty;
    }
}