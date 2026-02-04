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
        _logger.LogDebug("User {UserId} getting documents for world {WorldId}", user.Id, worldId);

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
    /// Get a download URL for a document.
    /// </summary>
    [HttpGet("documents/{documentId:guid}/content")]
    [HttpGet("/documents/{documentId:guid}/content")]
    public async Task<ActionResult<WorldDocumentDownloadDto>> GetDocumentContentAsync(Guid documentId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("User {UserId} requesting download URL for document {DocumentId}", user.Id, documentId);

        try
        {
            var result = await _documentService.GetDocumentContentAsync(documentId, user.Id);

            return Ok(new WorldDocumentDownloadDto
            {
                DownloadUrl = result.DownloadUrl,
                FileName = result.FileName,
                ContentType = result.ContentType,
                FileSizeBytes = result.FileSizeBytes
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document content request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found for content request");
            return NotFound(new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogDebug(ex, "File not found in storage");
            return NotFound(new { error = "File not found in storage" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL");
            return StatusCode(500, new { error = "Failed to generate download URL" });
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
        _logger.LogDebug("User {UserId} requesting document upload for world {WorldId}", user.Id, worldId);

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
        _logger.LogDebug("User {UserId} confirming document upload {DocumentId}", user.Id, documentId);

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
        _logger.LogDebug("User {UserId} updating document {DocumentId}", user.Id, documentId);

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
        _logger.LogDebug("User {UserId} deleting document {DocumentId}", user.Id, documentId);

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
