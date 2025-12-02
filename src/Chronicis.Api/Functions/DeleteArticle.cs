using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Chronicis.Api.Functions;

public class DeleteArticle
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleValidationService _validationService;
    private readonly ILogger<DeleteArticle> _logger;

    public DeleteArticle(
        ChronicisDbContext context,
        ArticleValidationService validationService,
        ILogger<DeleteArticle> logger)
    {
        _context = context;
        _validationService = validationService;
        _logger = logger;
    }

    [Function("DeleteArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "articles/{id}")] HttpRequestData req,
        FunctionContext context,
        int id)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("DeleteArticle {ArticleId} called by user {UserId}", id, user.Id);

        try
        {
            var validationResult = await _validationService.ValidateDeleteAsync(id);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteStringAsync(validationResult.GetFirstError());
                return validationError;
            }

            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

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
}
