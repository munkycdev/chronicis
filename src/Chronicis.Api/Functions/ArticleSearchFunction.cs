using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Net;
using System.Web;

namespace Chronicis.Api.Functions;

public class ArticleSearchFunction : BaseAuthenticatedFunction
{
    private readonly ChronicisDbContext _context;

    public ArticleSearchFunction(ChronicisDbContext context,
            ILogger<ArticleSearchFunction> logger,
            IUserService userService,
            IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger)
    {
        _context = context;
    }

    [Function("SearchArticles")]
    public async Task<HttpResponseData> SearchArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/search")]
        HttpRequestData req)
    {
        var logger = req.FunctionContext.GetLogger("SearchArticles");
        
        // Get query parameter
        var query = HttpUtility.ParseQueryString(req.Url.Query).Get("query");
        
        if (string.IsNullOrWhiteSpace(query))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Query parameter is required" });
            return badRequest;
        }
        
        logger.LogInformation("Searching for: {Query}", query);
        
        try
        {
            // Search titles
            var titleMatches = await _context.Articles
                .Where(a => EF.Functions.Like(a.Title, $"%{query}%"))
                .OrderBy(a => a.Title)
                .Take(20)
                .ToListAsync();
            
            logger.LogInformation("Found {Count} title matches", titleMatches.Count);
            
            // Search bodies (exclude articles already matched by title)
            var titleMatchIds = titleMatches.Select(a => a.Id).ToList();
            var bodyMatches = await _context.Articles
                .Where(a => EF.Functions.Like(a.Body, $"%{query}%") 
                         && !titleMatchIds.Contains(a.Id))
                .OrderByDescending(a => a.ModifiedDate ?? a.CreatedDate)
                .Take(20)
                .ToListAsync();
            
            logger.LogInformation("Found {Count} body matches", bodyMatches.Count);
            
            // Search hashtags
            var hashtagMatches = await _context.ArticleHashtags
                .Include(ah => ah.Article)
                .Include(ah => ah.Hashtag)
                .Where(ah => EF.Functions.Like(ah.Hashtag.Name, $"%{query}%"))
                .Select(ah => ah.Article)
                .Distinct()
                .OrderBy(a => a.Title)
                .Take(20)
                .ToListAsync();
            
            logger.LogInformation("Found {Count} hashtag matches", hashtagMatches.Count);
            
            // Build results with context snippets
            var results = new GlobalSearchResultsDto
            {
                Query = query,
                TitleMatches = await BuildSearchResults(titleMatches, query, "title"),
                BodyMatches = await BuildSearchResults(bodyMatches, query, "content"),
                HashtagMatches = await BuildSearchResults(hashtagMatches, query, "hashtag"),
                TotalResults = titleMatches.Count + bodyMatches.Count + hashtagMatches.Count
            };
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching articles");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    private async Task<List<ArticleSearchResultDto>> BuildSearchResults(
        List<Article> articles, 
        string query, 
        string matchType)
    {
        var results = new List<ArticleSearchResultDto>();
        
        foreach (var article in articles)
        {
            var snippet = ExtractSnippet(article?.Body ?? string.Empty, query, 200);
            var breadcrumbs = await BuildBreadcrumbs(article?.Id ?? 0);
            var slug = CreateSlug(article?.Title ?? string.Empty);
            
            results.Add(new ArticleSearchResultDto
            {
                Id = article?.Id ?? 0,
                Title = string.IsNullOrEmpty(article?.Title) ? "(Untitled)" : article.Title,
                Slug = slug,
                MatchSnippet = snippet,
                MatchType = matchType,
                AncestorPath = breadcrumbs,
                LastModified = article?.ModifiedDate ?? article?.CreatedDate ?? DateTime.MinValue,
            });
        }
        
        return results;
    }

    private string ExtractSnippet(string content, string query, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "";
            
        // Find the position of the query in the content
        var index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        
        if (index < 0)
        {
            // Query not found in content (shouldn't happen), return start
            return content.Length <= maxLength 
                ? content 
                : content.Substring(0, maxLength) + "...";
        }
        
        // Calculate snippet window
        var startBuffer = 50;
        var endBuffer = maxLength - query.Length - startBuffer;
        
        var start = Math.Max(0, index - startBuffer);
        var length = Math.Min(content.Length - start, maxLength);
        
        var snippet = content.Substring(start, length);
        
        // Add ellipsis
        if (start > 0)
            snippet = "..." + snippet;
        if (start + length < content.Length)
            snippet = snippet + "...";
            
        return snippet.Trim();
    }

    private async Task<List<BreadcrumbDto>> BuildBreadcrumbs(int articleId)
    {
        var breadcrumbs = new List<BreadcrumbDto>();
        var current = await _context.Articles.FindAsync(articleId);
        
        while (current != null)
        {
            breadcrumbs.Insert(0, new BreadcrumbDto
            {
                Id = current.Id,
                Title = string.IsNullOrEmpty(current.Title) ? "(Untitled)" : current.Title
            });
            
            if (current.ParentId.HasValue)
                current = await _context.Articles.FindAsync(current.ParentId.Value);
            else
                break;
        }
        
        return breadcrumbs;
    }

    private static string CreateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "untitled";
            
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(":", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Trim('-');
    }
}
