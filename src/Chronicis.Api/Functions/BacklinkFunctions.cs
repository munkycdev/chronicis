using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Functions;

public class BacklinkFunctions : ArticleBaseClass
{
    public BacklinkFunctions(ChronicisDbContext context) : base(context) { }

    /// <summary>
    /// GET /api/articles/{id}/backlinks
    /// Returns all articles that reference this article via hashtags
    /// </summary>
    [Function("GetArticleBacklinks")]
    public async Task<HttpResponseData> GetArticleBacklinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id}/backlinks")] 
        HttpRequestData req,
        int id)
    {
        var response = req.CreateResponse();

        try
        {
            // Get the target article
            var targetArticle = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id);

            if (targetArticle == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new { error = "Article not found" });
                return response;
            }

            // Find all hashtags that reference this article
            var relevantHashtags = await _context.Hashtags
                .Where(h => h.LinkedArticleId == id)
                .Select(h => h.Id)
                .ToListAsync();

            if (!relevantHashtags.Any())
            {
                // No hashtags link to this article, return empty list
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new List<BacklinkDto>());
                return response;
            }

            // Find all articles that use these hashtags (excluding the target article itself)
            var backlinks = await _context.ArticleHashtags
                .Include(ah => ah.Article)
                .Include(ah => ah.Hashtag)
                .Where(ah => relevantHashtags.Contains(ah.HashtagId) && ah.ArticleId != id)
                .GroupBy(ah => ah.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    Article = g.First().Article,
                    Hashtags = g.Select(ah => ah.Hashtag.Name).ToList(),
                    MentionCount = g.Count()
                })
                .ToListAsync();

            var backlinkDtos = backlinks.Select(b => new BacklinkDto
            {
                ArticleId = b.ArticleId,
                ArticleTitle = b.Article.Title,
                ArticleSlug = CreateSlug(b.Article.Title),
                Hashtags = b.Hashtags,
                MentionCount = b.MentionCount,
                LastModified = b.Article.ModifiedDate ?? b.Article.CreatedDate
            }).OrderByDescending(b => b.LastModified).ToList();

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(backlinkDtos);
        }
        catch (Exception ex)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ex.Message });
        }

        return response;
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
            .Replace("\"", "");
    }
}
