using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class LinkSuggestionsFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<LinkSuggestionsFunctions> _logger;

    public LinkSuggestionsFunctions(
        ChronicisDbContext context,
        ILogger<LinkSuggestionsFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("GetLinkSuggestions")]
    public async Task<HttpResponseData> GetLinkSuggestions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/link-suggestions")] HttpRequestData req,
        FunctionContext context,
        Guid worldId)
    {
        var user = context.GetRequiredUser();

        try
        {
            // Get query parameter
            var query = req.Query["query"];

            // Return empty if query is less than 3 characters
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(new LinkSuggestionsResponseDto
                {
                    Suggestions = new List<LinkSuggestionDto>()
                });
                return emptyResponse;
            }

            // Parse query into path filter segments and search term
            // Examples:
            //   "plen" -> pathFilters: [], searchTerm: "plen"
            //   "characters/plen" -> pathFilters: ["characters"], searchTerm: "plen"
            //   "characters/npcs/plen" -> pathFilters: ["characters", "npcs"], searchTerm: "plen"
            //   "characters/" -> pathFilters: ["characters"], searchTerm: ""
            var (pathFilters, searchTerm) = ParseQuery(query);

            _logger.LogInformation(
                "Link suggestions query: raw='{Query}', pathFilters=[{PathFilters}], searchTerm='{SearchTerm}'",
                query, string.Join(", ", pathFilters), searchTerm);

            // Get all articles in this world
            var articles = await _context.Articles
                .Where(a => a.WorldId == worldId && a.CreatedBy == user.Id)
                .Select(a => new ArticlePathInfo
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,
                    Type = a.Type,
                    ParentId = a.ParentId
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} articles in world {WorldId} for user {UserId}", 
                articles.Count, worldId, user.Id);

            // Build full paths for each article
            var articlesWithPaths = articles
                .Select(a => new
                {
                    Article = a,
                    PathSegments = BuildPathSegments(a.Id, a.Title, articles)
                })
                .ToList();

            // Filter by path segments and search term
            var filtered = articlesWithPaths.Where(a =>
            {
                if (pathFilters.Count > 0)
                {
                    // Find if pathFilters match any contiguous subsequence in the article's path
                    // e.g., pathFilters ["characters"] should match path ["Wiki", "Characters", "Adria"]
                    // because "Characters" exists in the path
                    
                    // We need at least (pathFilters.Count + 1) segments (filters + the article itself)
                    if (a.PathSegments.Count <= pathFilters.Count)
                        return false;

                    // Try to find where the pathFilters match in the path
                    // The filters must match contiguous segments, and the article title comes after
                    bool foundMatch = false;
                    
                    // Try each possible starting position
                    // The last segment is the article title, so filters can start from 0 to (Count - filters.Count - 1)
                    for (int startIdx = 0; startIdx <= a.PathSegments.Count - pathFilters.Count - 1; startIdx++)
                    {
                        bool allMatch = true;
                        for (int i = 0; i < pathFilters.Count; i++)
                        {
                            if (!string.Equals(a.PathSegments[startIdx + i], pathFilters[i], StringComparison.OrdinalIgnoreCase))
                            {
                                allMatch = false;
                                break;
                            }
                        }
                        
                        if (allMatch)
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    
                    if (!foundMatch)
                        return false;
                }

                // If there's a search term, the article title (last segment) must start with it
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    return a.Article.Title.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase);
                }

                // If no search term (e.g., "characters/"), show all matches
                return true;
            });

            // Build suggestions with full display path
            var suggestions = filtered
                .Select(a => new LinkSuggestionDto
                {
                    ArticleId = a.Article.Id,
                    Title = a.Article.Title,
                    DisplayPath = string.Join(" / ", a.PathSegments),
                    ArticleType = a.Article.Type,
                    Slug = a.Article.Slug
                })
                .OrderBy(s => s.DisplayPath)
                .Take(10)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new LinkSuggestionsResponseDto
            {
                Suggestions = suggestions
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting link suggestions for world {WorldId}", worldId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error getting link suggestions: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// Parses a query string into path filter segments and a search term.
    /// Uses "/" as the separator.
    /// </summary>
    private (List<string> pathFilters, string searchTerm) ParseQuery(string query)
    {
        var parts = query.Split('/', StringSplitOptions.None);
        
        if (parts.Length == 1)
        {
            // No slashes - just a search term
            return (new List<string>(), query);
        }

        // Everything except the last part is a path filter
        var pathFilters = parts
            .Take(parts.Length - 1)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        // The last part is the search term (could be empty if query ends with "/")
        var searchTerm = parts.Last();

        return (pathFilters, searchTerm);
    }

    /// <summary>
    /// Builds the full path segments for an article by walking up the parent chain.
    /// Strips the World name (first segment) since we're already filtering by WorldId.
    /// Returns list from root to leaf, e.g., ["Wiki", "Characters", "Adria"]
    /// </summary>
    private List<string> BuildPathSegments(Guid articleId, string articleTitle, List<ArticlePathInfo> allArticles)
    {
        var path = new List<string>();
        var currentId = articleId;
        var currentTitle = articleTitle;

        // Walk up the parent chain, building path in reverse
        while (true)
        {
            path.Insert(0, currentTitle);

            var current = allArticles.FirstOrDefault(a => a.Id == currentId);
            if (current == null || current.ParentId == null)
            {
                break;
            }

            var parent = allArticles.FirstOrDefault(a => a.Id == current.ParentId.Value);
            if (parent == null)
            {
                break;
            }

            currentId = parent.Id;
            currentTitle = parent.Title;
        }

        // Strip the first segment (World name) since we're already scoped by WorldId
        if (path.Count > 1)
        {
            path.RemoveAt(0);
        }

        return path;
    }

    /// <summary>
    /// Helper class for building article paths.
    /// </summary>
    private class ArticlePathInfo
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public ArticleType Type { get; set; }
        public Guid? ParentId { get; set; }
    }
}
