using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class DeleteArticle
{
    private readonly ChronicisDbContext _context;
    private readonly IArticleValidationService _validationService;
    private readonly ILogger<DeleteArticle> _logger;

    public DeleteArticle(
        ChronicisDbContext context,
        IArticleValidationService validationService,
        ILogger<DeleteArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _logger = logger;
    }

    [Function("DeleteArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "articles/{id:guid}")] HttpRequestData req,
        FunctionContext context,
        Guid id)
    {
        var user = context.GetRequiredUser();

        try
        {
            var validationResult = await _validationService.ValidateDeleteAsync(id);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteStringAsync(validationResult.GetFirstError());
                return validationError;
            }

            // Get article if user has access via world membership (same pattern as UpdateArticle)
            var article = await _context.Articles
                .Where(a => a.Id == id)
                .Where(a => a.WorldId.HasValue && 
                            _context.WorldMembers.Any(wm => wm.WorldId == a.WorldId.Value && wm.UserId == user.Id))
                .FirstOrDefaultAsync();

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            // Get all descendant article IDs (recursive)
            var allArticleIds = await GetAllDescendantIdsAsync(id);
            allArticleIds.Add(id); // Include the article itself

            _logger.LogInformation("Deleting article {ArticleId} and {DescendantCount} descendants", 
                id, allArticleIds.Count - 1);

            // Delete all articles (children first, then parent - order by depth descending)
            var articlesToDelete = await _context.Articles
                .Where(a => allArticleIds.Contains(a.Id))
                .ToListAsync();

            // Sort by depth (deepest first) to avoid FK constraint issues
            var sortedArticles = await SortByDepthDescendingAsync(articlesToDelete);
            _context.Articles.RemoveRange(sortedArticles);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted article {ArticleId} and {TotalCount} total articles", 
                id, allArticleIds.Count);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting article {ArticleId}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error deleting article: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// Recursively gets all descendant article IDs
    /// </summary>
    private async Task<HashSet<Guid>> GetAllDescendantIdsAsync(Guid parentId)
    {
        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            
            var childIds = await _context.Articles
                .Where(a => a.ParentId == currentId)
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                if (result.Add(childId))
                {
                    queue.Enqueue(childId);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Sorts articles so that children come before parents (deepest first)
    /// </summary>
    private async Task<List<Article>> SortByDepthDescendingAsync(List<Article> articles)
    {
        var articleDict = articles.ToDictionary(a => a.Id);
        var depths = new Dictionary<Guid, int>();

        foreach (var article in articles)
        {
            depths[article.Id] = await CalculateDepthAsync(article.Id, articleDict);
        }

        return articles.OrderByDescending(a => depths[a.Id]).ToList();
    }

    private async Task<int> CalculateDepthAsync(Guid articleId, Dictionary<Guid, Article> articleDict)
    {
        int depth = 0;
        var currentId = articleId;

        while (articleDict.TryGetValue(currentId, out var article) && article.ParentId.HasValue)
        {
            depth++;
            currentId = article.ParentId.Value;
            
            // If parent is not in our delete set, get it from DB
            if (!articleDict.ContainsKey(currentId))
            {
                var parent = await _context.Articles
                    .Where(a => a.Id == currentId)
                    .Select(a => new { a.Id, a.ParentId })
                    .FirstOrDefaultAsync();
                
                if (parent == null) break;
                
                // Continue traversing up
                while (parent.ParentId.HasValue)
                {
                    depth++;
                    var nextParent = await _context.Articles
                        .Where(a => a.Id == parent.ParentId.Value)
                        .Select(a => new { a.Id, a.ParentId })
                        .FirstOrDefaultAsync();
                    
                    if (nextParent == null) break;
                    parent = nextParent;
                }
                break;
            }
        }

        return depth;
    }
}
