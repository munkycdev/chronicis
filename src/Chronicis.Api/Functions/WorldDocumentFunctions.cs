using System.Net;
using System.Text.Json;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for World Document management (file uploads to blob storage)
/// </summary>
public class WorldDocumentFunctions
{
    private readonly IWorldDocumentService _documentService;
    private readonly ILogger<WorldDocumentFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorldDocumentFunctions(
        IWorldDocumentService documentService,
        ILogger<WorldDocumentFunctions> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Request a document upload (generates SAS URL for client upload)
    /// POST /api/worlds/{worldId}/documents/request-upload
    /// </summary>
    [Function("RequestDocumentUpload")]
    public async Task<HttpResponseData> RequestDocumentUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "worlds/{worldId:guid}/documents/request-upload")] 
        HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} requesting document upload for world {WorldId}", 
            user.Id, worldId);

        try
        {
            var request = await JsonSerializer.DeserializeAsync<WorldDocumentUploadRequestDto>(
                req.Body, _jsonOptions);

            if (request == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badRequest;
            }

            var result = await _documentService.RequestUploadAsync(worldId, user.Id, request);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized upload request");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = ex.Message });
            return badRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting document upload");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to request upload" });
            return error;
        }
    }

    /// <summary>
    /// Confirm a document upload completed successfully
    /// POST /api/worlds/{worldId}/documents/{documentId}/confirm
    /// </summary>
    [Function("ConfirmDocumentUpload")]
    public async Task<HttpResponseData> ConfirmDocumentUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "worlds/{worldId:guid}/documents/{documentId:guid}/confirm")] 
        HttpRequestData req,
        Guid worldId,
        Guid documentId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} confirming document upload {DocumentId}", 
            user.Id, documentId);

        try
        {
            var result = await _documentService.ConfirmUploadAsync(worldId, documentId, user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized confirm upload");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid confirm upload request");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = ex.Message });
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming document upload");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to confirm upload" });
            return error;
        }
    }

    /// <summary>
    /// Get all documents for a world
    /// GET /api/worlds/{worldId}/documents
    /// </summary>
    [Function("GetWorldDocuments")]
    public async Task<HttpResponseData> GetWorldDocuments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/documents")] 
        HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} getting documents for world {WorldId}", 
            user.Id, worldId);

        try
        {
            var documents = await _documentService.GetWorldDocumentsAsync(worldId, user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(documents);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to world documents");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting world documents");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to get documents" });
            return error;
        }
    }

    /// <summary>
    /// Get download URL for a document (generates time-limited SAS token)
    /// GET /api/worlds/{worldId}/documents/{documentId}/download
    /// </summary>
    [Function("GetDocumentDownloadUrl")]
    public async Task<HttpResponseData> GetDocumentDownloadUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/documents/{documentId:guid}/download")] 
        HttpRequestData req,
        Guid worldId,
        Guid documentId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} requesting download URL for document {DocumentId}", 
            user.Id, documentId);

        try
        {
            var result = await _documentService.GetDownloadUrlAsync(worldId, documentId, user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized download request");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for download");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = ex.Message });
            return notFound;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Blob not found for document");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "File not found in storage" });
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to generate download URL" });
            return error;
        }
    }

    /// <summary>
    /// Update document metadata (title, description)
    /// PUT /api/worlds/{worldId}/documents/{documentId}
    /// </summary>
    [Function("UpdateWorldDocument")]
    public async Task<HttpResponseData> UpdateWorldDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "worlds/{worldId:guid}/documents/{documentId:guid}")] 
        HttpRequestData req,
        Guid worldId,
        Guid documentId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} updating document {DocumentId}", 
            user.Id, documentId);

        try
        {
            var update = await JsonSerializer.DeserializeAsync<WorldDocumentUpdateDto>(
                req.Body, _jsonOptions);

            if (update == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badRequest;
            }

            var result = await _documentService.UpdateDocumentAsync(worldId, documentId, user.Id, update);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update request");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for update");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = ex.Message });
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to update document" });
            return error;
        }
    }

    /// <summary>
    /// Delete a document (removes from database and blob storage)
    /// DELETE /api/worlds/{worldId}/documents/{documentId}
    /// </summary>
    [Function("DeleteWorldDocument")]
    public async Task<HttpResponseData> DeleteWorldDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "worlds/{worldId:guid}/documents/{documentId:guid}")] 
        HttpRequestData req,
        Guid worldId,
        Guid documentId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} deleting document {DocumentId}", 
            user.Id, documentId);

        try
        {
            await _documentService.DeleteDocumentAsync(worldId, documentId, user.Id);

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete request");
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            return forbidden;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for deletion");
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = ex.Message });
            return notFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = "Failed to delete document" });
            return error;
        }
    }
}
