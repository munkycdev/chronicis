using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace Chronicis.Api.Functions;

public class ArticleSearchFunction
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ArticleSearchFunction> _logger;

    public ArticleSearchFunction(
        ChronicisDbContext context,
        ILogger<ArticleSearchFunction> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("SearchArticles")]
    public async Task<HttpResponseData> SearchArticles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/search")]
        HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        
        var query = HttpUtility.ParseQueryString(req.Url.Query).Get("query");
        
        if (string.IsNullOrWhiteSpace(query))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Query parameter is required" });
            return badRequest;
        }
        
        _logger.LogInformation("Searching for: {Query} by user {UserId}", query, user.Id);
        
        try
        {
            // Search titles
            var titleMatches = await _context.Articles
                .Where(a => a.UserId == user.Id && EF.Functions.Like(a.Title, $"%{query}%"))
                .OrderBy(a => a.Title)
                .Take(20)
                .ToListAsync();
            
            // Search bodies (exclude articles already matched by title)
            var titleMatchIds = titleMatches.Select(a => a.Id).ToList();
            var bodyMatches = await _context.Articles
                .Where(a => a.UserId == user.Id 
                         && EF.Functions.Like(a.Body, $"%{query}%") 
                         && !titleMatchIds.Contains(a.Id))
                .OrderByDescending(a => a.ModifiedDate ?? a.CreatedDate)
                .Take(20)
                .ToListAsync();
            
            // Search hashtags
            var hashtagMatches = await _context.ArticleHashtags
                .Include(ah => ah.Article)
                .Include(ah => ah.Hashtag)
                .Where(ah => ah.Article.UserId == user.Id 
                          && EF.Functions.Like(ah.Hashtag.Name, $"%{query}%"))
                .Select(ah => ah.Article)
                .Distinct()
                .OrderBy(a => a.Title)
                .Take(20)
                .ToListAsync();
            
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
            _logger.LogError(ex, "Error searching articles");
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
            var slug = SlugUtility.CreateSlug(article?.Title);
            
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
            
        var index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        
        if (index < 0)
        {
            return content.Length <= maxLength 
                ? content 
                : content.Substring(0, maxLength) + "...";
        }
        
        var startBuffer = 50;
        var start = Math.Max(0, index - startBuffer);
        var length = Math.Min(content.Length - start, maxLength);
        
        var snippet = content.Substring(start, length);
        
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
}
