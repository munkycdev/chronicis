using System.Net;
using System.Text.Json;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

public class AutoHashtagFunction
{
    private readonly IAutoHashtagService _autoHashtagService;
    private readonly ILogger<AutoHashtagFunction> _logger;

    public AutoHashtagFunction(
        IAutoHashtagService autoHashtagService,
        ILogger<AutoHashtagFunction> logger)
    {
        _autoHashtagService = autoHashtagService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/articles/auto-hashtag
    /// Automatically find and insert hashtags based on article title references
    /// </summary>
    [Function("AutoHashtag")]
    public async Task<HttpResponseData> AutoHashtag(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "articles/auto-hashtag")]
        HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        var response = req.CreateResponse();

        try
        {
            var request = await JsonSerializer.DeserializeAsync<AutoHashtagRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (request == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(new { error = "Invalid request body" });
                return response;
            }

            var result = await _autoHashtagService.ProcessArticlesAsync(
                user.Id,
                request.DryRun,
                request.ArticleIds
            );

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(result);

            _logger.LogInformation(
                "Auto-hashtag processed {ArticleCount} articles for user {UserId}, found {MatchCount} matches (DryRun: {DryRun})",
                result.TotalArticlesScanned,
                user.Id,
                result.TotalMatchesFound,
                request.DryRun
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auto-hashtag request");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { error = ex.Message });
        }

        return response;
    }
}
