using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http; // Add this
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web; // For query string parsing

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
            // Parse query string
            var queryString = HttpUtility.ParseQueryString(req.Url.Query);
            var query = queryString["query"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                var emptyResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new List<ArticleSearchResultDto>());
                return emptyResponse;
            }

            _logger.LogInformation("Searching articles with query: {Query}", query);

            // Search for articles with titles containing the query (case-insensitive)
            var matchingArticles = await _context.Articles
                .Where(a => EF.Functions.Like(a.Title, $"%{query}%"))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.ParentId,
                    a.CreatedDate,
                    a.ModifiedDate
                })
                .ToListAsync();

            // Build results with ancestor paths
            var results = new List<ArticleSearchResultDto>();

            foreach (var article in matchingArticles)
            {
                var ancestorPath = await BuildAncestorPath(article.Id);

                results.Add(new ArticleSearchResultDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    ParentId = article.ParentId,
                    CreatedDate = article.CreatedDate,
                    ModifiedDate = article.ModifiedDate,
                    AncestorPath = ancestorPath
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

    private async Task<List<AncestorDto>> BuildAncestorPath(int articleId)
    {
        var ancestors = new List<AncestorDto>();
        var currentId = articleId;

        while (true)
        {
            var article = await _context.Articles
                .Where(a => a.Id == currentId)
                .Select(a => new { a.Id, a.Title, a.ParentId })
                .FirstOrDefaultAsync();

            if (article == null)
                break;

            ancestors.Insert(0, new AncestorDto
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
}