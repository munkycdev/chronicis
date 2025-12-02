using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

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

    [Function("GetArticleBacklinks")]
    public async Task<HttpResponseData> GetArticleBacklinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{id}/backlinks")]
        HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        var response = req.CreateResponse();

        try
        {
            var targetArticle = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (targetArticle == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new { error = "Article not found" });
                return response;
            }

            var relevantHashtags = await _context.Hashtags
                .Where(h => h.LinkedArticleId == id)
                .Select(h => h.Id)
                .ToListAsync();

            if (!relevantHashtags.Any())
            {
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new List<BacklinkDto>());
                return response;
            }

            var backlinks = await _context.ArticleHashtags
                .Include(ah => ah.Article)
                .Include(ah => ah.Hashtag)
                .Where(ah => relevantHashtags.Contains(ah.HashtagId)
                          && ah.ArticleId != id
                          && ah.Article.UserId == user.Id)
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
                ArticleSlug = SlugUtility.CreateSlug(b.Article.Title),
                Hashtags = b.Hashtags,
                MentionCount = b.MentionCount,
                LastModified = b.Article.ModifiedDate ?? b.Article.CreatedDate
            }).OrderByDescending(b => b.LastModified).ToList();

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(backlinkDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backlinks for article {ArticleId}", id);
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ex.Message });
        }

        return response;
    }
}
