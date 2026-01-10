using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions.ExternalLinks;

public class ExternalLinksFunctions
{
    private readonly ExternalLinkSuggestionService _suggestionService;
    private readonly ExternalLinkContentService _contentService;
    private readonly ExternalLinkValidationService _validationService;
    private readonly ILogger<ExternalLinksFunctions> _logger;

    public ExternalLinksFunctions(
        ExternalLinkSuggestionService suggestionService,
        ExternalLinkContentService contentService,
        ExternalLinkValidationService validationService,
        ILogger<ExternalLinksFunctions> logger)
    {
        _suggestionService = suggestionService;
        _contentService = contentService;
        _validationService = validationService;
        _logger = logger;
    }

    [Function("GetExternalLinkSuggestions")]
    public async Task<HttpResponseData> GetExternalLinkSuggestions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "external-links/suggestions")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        try
        {
            var source = req.Query["source"];
            var query = req.Query["query"];

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(query))
            {
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                await emptyResponse.WriteAsJsonAsync(Array.Empty<ExternalLinkSuggestionDto>());
                return emptyResponse;
            }

            if (!_validationService.TryValidateSource(source, out var sourceError))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ExternalLinkErrorDto { Message = sourceError });
                return errorResponse;
            }

            var suggestions = await _suggestionService.GetSuggestionsAsync(
                source,
                query,
                context.CancellationToken);

                _logger.LogInformation("Suggestions returned by the service: " + suggestions.Count());

            var responseDtos = suggestions
                .Select(s => new ExternalLinkSuggestionDto
                {
                    Source = s.Source,
                    Id = s.Id,
                    Title = s.Title,
                    Subtitle = s.Subtitle,
                    Icon = s.Icon,
                    Href = s.Href
                })
                .ToList();

            if (!responseDtos.Any())
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { "responseCount", suggestions.Count()});
                return response;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseDtos);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external link suggestions");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(Array.Empty<ExternalLinkSuggestionDto>());
            return errorResponse;
        }
    }

    [Function("GetExternalLinkContent")]
    public async Task<HttpResponseData> GetExternalLinkContent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "external-links/content")] HttpRequestData req,
        FunctionContext context)
    {
        if (context.GetUser() == null)
        {
            return await CreateUnauthorizedResponseAsync(req);
        }

        try
        {
            var source = req.Query["source"];
            var id = req.Query["id"];

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new ExternalLinkErrorDto { Message = "Source and id are required." });
                return badRequestResponse;
            }

            if (!_validationService.TryValidateSource(source, out var sourceError))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new ExternalLinkErrorDto { Message = sourceError });
                return badRequestResponse;
            }

            if (!_validationService.TryValidateId(source, id, out var idError))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new ExternalLinkErrorDto { Message = idError });
                return badRequestResponse;
            }

            var content = await _contentService.GetContentAsync(source, id, context.CancellationToken);
            if (content == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new ExternalLinkContentDto());
                return notFoundResponse;
            }

            var responseDto = new ExternalLinkContentDto
            {
                Source = content.Source,
                Id = content.Id,
                Title = content.Title,
                Kind = content.Kind,
                Markdown = content.Markdown,
                Attribution = content.Attribution,
                ExternalUrl = content.ExternalUrl
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseDto);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external link content");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ExternalLinkContentDto());
            return errorResponse;
        }
    }

    private static async Task<HttpResponseData> CreateUnauthorizedResponseAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = "Authentication required. Please provide a valid Auth0 token." });
        return response;
    }
}
