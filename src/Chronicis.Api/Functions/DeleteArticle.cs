using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
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

            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.CreatedBy == user.Id);

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
