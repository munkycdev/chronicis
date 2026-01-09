using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for document content streaming.
/// </summary>
public class DocumentContentFunctions
{
    private readonly IWorldDocumentService _documentService;
    private readonly ILogger<DocumentContentFunctions> _logger;

    public DocumentContentFunctions(
        IWorldDocumentService documentService,
        ILogger<DocumentContentFunctions> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Stream document content for an authorized user.
    /// GET /api/documents/{id}/content
    /// </summary>
    [Function("GetDocumentContent")]
    public async Task<HttpResponseData> GetDocumentContent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/{id:guid}/content")]
        HttpRequestData req,
        Guid id,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} requesting document content {DocumentId}", user.Id, id);

        try
        {
            var result = await _documentService.GetDocumentContentAsync(id, user.Id);
            var response = req.CreateResponse(HttpStatusCode.OK);
            var safeFileName = result.FileName.Replace("\"", "'").Replace("\r", "").Replace("\n", "");

            response.Headers.Add("Content-Type", result.ContentType);
            response.Headers.Add("Content-Disposition", $"inline; filename=\"{safeFileName}\"");

            if (result.ContentLength.HasValue)
            {
                response.Headers.Add("Content-Length", result.ContentLength.Value.ToString());
            }

            await using var contentStream = result.Content;
            await contentStream.CopyToAsync(response.Body);

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document content request");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for content request");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = ex.Message });
            return notFound;
        }
        catch (FileNotFoundException)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "File not found in storage" });
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming document content");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to stream document content" });
            return error;
        }
    }
}
