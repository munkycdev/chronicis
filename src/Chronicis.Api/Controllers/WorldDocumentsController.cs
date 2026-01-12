using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for World Document management (file uploads to blob storage).
/// </summary>
[Route("worlds/{worldId:guid}/documents")]
[Authorize]
public class WorldDocumentsController : ControllerBase
{
    private readonly IWorldDocumentService _documentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorldDocumentsController> _logger;

    public WorldDocumentsController(
        IWorldDocumentService documentService,
        ICurrentUserService currentUserService,
        ILogger<WorldDocumentsController> logger)
    {
        _documentService = documentService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /worlds/{worldId}/documents - Get all documents for a world.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorldDocumentDto>>> GetWorldDocuments(Guid worldId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} getting documents for world {WorldId}", user.Id, worldId);

        try
        {
            var documents = await _documentService.GetWorldDocumentsAsync(worldId, user.Id);
            return Ok(documents);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to world documents");
            return StatusCode(403, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Streams the content of a document to the HTTP response.
    /// </summary>
    [HttpGet("/documents/{documentId:guid}/content")]
    public async Task<HttpResponse> GetDocumentContentAsync(Guid documentId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} requesting document content {DocumentId}", user.Id, documentId);

        try
        {
            var result = await _documentService.GetDocumentContentAsync(documentId, user.Id);
            var safeFileName = result.FileName.Replace("\"", "'").Replace("\r", "").Replace("\n", "");

            var response = HttpContext.Response;

            response.StatusCode = (int)HttpStatusCode.OK;

            response.Headers.Clear();
            response.Headers.Append("Content-Type", result.ContentType);
            response.Headers.Append("Content-Disposition", $"inline; filename=\"{safeFileName}\"");

            if (result.ContentLength.HasValue)
            {
                response.Headers.Append("Content-Length", result.ContentLength.Value.ToString());
            }

            await using var contentStream = result.Content;
            await contentStream.CopyToAsync(response.Body);

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document content request");
            //var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            //await forbidden.WriteAsJsonAsync(new { error = ex.Message });
            //return forbidden;
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for content request");
            //var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            //await notFound.WriteAsJsonAsync(new { error = ex.Message });
            //return notFound;
            throw;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogInformation(ex, "File not found in storage");
            //var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            //await notFound.WriteAsJsonAsync(new { error = "File not found in storage" });
            //return notFound;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming document content");
            //var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            //await error.WriteAsJsonAsync(new { error = "Failed to stream document content" });
            //return error;
            throw;
        }
    }

    /// <summary>
    /// POST /worlds/{worldId}/documents/request-upload - Request a document upload (generates SAS URL).
    /// </summary>
    [HttpPost("request-upload")]
    public async Task<ActionResult<WorldDocumentUploadResponseDto>> RequestDocumentUpload(
        Guid worldId,
        [FromBody] WorldDocumentUploadRequestDto request)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} requesting document upload for world {WorldId}", user.Id, worldId);

        if (request == null)
        {
            return BadRequest(new { error = "Invalid request body" });
        }

        try
        {
            var result = await _documentService.RequestUploadAsync(worldId, user.Id, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized upload request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /worlds/{worldId}/documents/{documentId}/confirm - Confirm a document upload completed.
    /// </summary>
    [HttpPost("{documentId:guid}/confirm")]
    public async Task<ActionResult<WorldDocumentDto>> ConfirmDocumentUpload(Guid worldId, Guid documentId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} confirming document upload {DocumentId}", user.Id, documentId);

        try
        {
            var result = await _documentService.ConfirmUploadAsync(worldId, documentId, user.Id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized confirm upload");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid confirm upload request");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /worlds/{worldId}/documents/{documentId} - Update document metadata.
    /// </summary>
    [HttpPut("{documentId:guid}")]
    public async Task<ActionResult<WorldDocumentDto>> UpdateWorldDocument(
        Guid worldId,
        Guid documentId,
        [FromBody] WorldDocumentUpdateDto update)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} updating document {DocumentId}", user.Id, documentId);

        if (update == null)
        {
            return BadRequest(new { error = "Invalid request body" });
        }

        try
        {
            var result = await _documentService.UpdateDocumentAsync(worldId, documentId, user.Id, update);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized update request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for update");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /worlds/{worldId}/documents/{documentId} - Delete a document.
    /// </summary>
    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> DeleteWorldDocument(Guid worldId, Guid documentId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} deleting document {DocumentId}", user.Id, documentId);

        try
        {
            await _documentService.DeleteDocumentAsync(worldId, documentId, user.Id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized delete request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for deletion");
            return NotFound(new { error = ex.Message });
        }
    }
}
