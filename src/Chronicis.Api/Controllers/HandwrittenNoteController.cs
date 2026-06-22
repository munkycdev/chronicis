using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Request DTO for handwritten note upload endpoints.
/// </summary>
public class HandwrittenNoteUploadRequest
{
    public byte[] ImageBytes { get; set; } = [];
}

/// <summary>
/// API endpoints for handwritten note operations on session note articles.
/// </summary>
[ApiController]
[Route("articles/{articleId:guid}/handwritten-note")]
[Authorize]
public class HandwrittenNoteController : ControllerBase
{
    private readonly IHandwrittenNoteService _handwrittenNoteService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<HandwrittenNoteController> _logger;

    public HandwrittenNoteController(
        IHandwrittenNoteService handwrittenNoteService,
        ICurrentUserService currentUserService,
        ILogger<HandwrittenNoteController> logger)
    {
        _handwrittenNoteService = handwrittenNoteService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// POST articles/{articleId}/handwritten-note — Upload or replace a handwritten note PNG.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HandwrittenNoteSaveResultDto>> SaveHandwrittenNote(
        Guid articleId,
        [FromBody] HandwrittenNoteUploadRequest request)
    {
        if (request?.ImageBytes == null || request.ImageBytes.Length == 0)
        {
            return BadRequest(new { error = "Image data is required and cannot be empty" });
        }

        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} saving handwritten note for article {ArticleId}", user.Id, articleId);

        try
        {
            var result = await _handwrittenNoteService.SaveAsync(articleId, user.Id, request.ImageBytes);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Article not found for handwritten note save");
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized handwritten note save");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to save handwritten note for article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to save handwritten note" });
        }
    }

    /// <summary>
    /// POST articles/{articleId}/handwritten-note/transcribe — Save and transcribe a handwritten note.
    /// </summary>
    [HttpPost("transcribe")]
    public async Task<ActionResult<HandwrittenNoteTranscribeResultDto>> TranscribeHandwrittenNote(
        Guid articleId,
        [FromBody] HandwrittenNoteUploadRequest request,
        [FromQuery] bool confirmOverwrite = false)
    {
        if (request?.ImageBytes == null || request.ImageBytes.Length == 0)
        {
            return BadRequest(new { error = "Image data is required and cannot be empty" });
        }

        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} transcribing handwritten note for article {ArticleId}", user.Id, articleId);

        try
        {
            var result = await _handwrittenNoteService.TranscribeAsync(articleId, user.Id, request.ImageBytes);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Transcription failed for article {ArticleId}", articleId);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized transcription request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to transcribe handwritten note for article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to transcribe handwritten note" });
        }
    }

    /// <summary>
    /// POST articles/{articleId}/handwritten-note/transcribe-existing — Transcribe an already-saved handwritten note.
    /// </summary>
    [HttpPost("transcribe-existing")]
    public async Task<ActionResult<HandwrittenNoteTranscribeResultDto>> TranscribeExistingHandwrittenNote(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} transcribing existing handwritten note for article {ArticleId}", user.Id, articleId);

        try
        {
            var result = await _handwrittenNoteService.TranscribeExistingAsync(articleId, user.Id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Transcription of existing note failed for article {ArticleId}", articleId);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized transcription request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to transcribe existing handwritten note for article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to transcribe handwritten note" });
        }
    }

    /// <summary>
    /// GET articles/{articleId}/handwritten-note — Get download URL for the handwritten note image.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetHandwrittenNoteUrl(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} getting handwritten note URL for article {ArticleId}", user.Id, articleId);

        try
        {
            var url = await _handwrittenNoteService.GetImageDownloadUrlAsync(articleId, user.Id);

            if (url == null)
            {
                return NotFound(new { error = "No handwritten note exists for this article" });
            }

            return Ok(new { downloadUrl = url });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Article not found for handwritten note URL");
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized handwritten note URL request");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to get handwritten note URL for article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to get handwritten note URL" });
        }
    }

    /// <summary>
    /// DELETE api/articles/{articleId}/handwritten-note — Delete the handwritten note image.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteHandwrittenNote(Guid articleId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogTraceSanitized("User {UserId} deleting handwritten note for article {ArticleId}", user.Id, articleId);

        try
        {
            await _handwrittenNoteService.DeleteAsync(articleId, user.Id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarningSanitized(ex, "Article not found for handwritten note deletion");
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarningSanitized(ex, "Unauthorized handwritten note deletion");
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to delete handwritten note for article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to delete handwritten note" });
        }
    }
}
