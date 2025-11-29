using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Chronicis.Api.Functions;

public class DeleteArticle : BaseAuthenticatedFunction
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleValidationService _validationService;

    public DeleteArticle(ChronicisDbContext context, 
            ArticleValidationService validationService,
            ILogger<DeleteArticle> logger,
            IUserService userService,
            IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger)
    {
        _context = context;
        _validationService = validationService;
    }

    [Function("DeleteArticle")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "articles/{id}")] HttpRequestData req,
        int id)
    {
        try
        {
            var (user, authErrorResponse) = await AuthenticateRequestAsync(req);
            if (authErrorResponse != null) return authErrorResponse;

            // Validate
            var validationResult = await _validationService.ValidateDeleteAsync(id);
            if (!validationResult.IsValid)
            {
                var validationError = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationError.WriteStringAsync(validationResult.GetFirstError());
                return validationError;
            }

            // Get article
            var article = await _context.Articles.FindAsync(id);
            
            if (article == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Article not found");
                return notFound;
            }

            // Delete article
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error deleting article: {ex.Message}");
            return errorResponse;
        }
    }
}
