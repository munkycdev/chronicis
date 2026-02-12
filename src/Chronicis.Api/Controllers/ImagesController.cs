using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Proxy endpoint for serving inline article images.
/// Resolves document IDs to fresh SAS download URLs via 302 redirect.
/// This avoids storing expiring SAS URLs in article HTML content.
/// </summary>
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly ChronicisDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        ChronicisDbContext db,
        IBlobStorageService blobStorage,
        ICurrentUserService currentUserService,
        ILogger<ImagesController> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/images/{documentId} - Redirect to a fresh SAS download URL for the image.
    /// Authenticated users who are members of (or own) the world can access images.
    /// </summary>
    [HttpGet("{documentId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetImage(Guid documentId)
    {
        var user = await _currentUserService.GetRequiredUserAsync();

        var document = await _db.WorldDocuments
            .AsNoTracking()
            .Include(d => d.World)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return NotFound();
        }

        // Check access: user must be the world owner or a member
        var hasAccess = document.World.OwnerId == user.Id
            || await _db.WorldMembers.AnyAsync(m => m.WorldId == document.WorldId && m.UserId == user.Id);

        if (!hasAccess)
        {
            return Forbid();
        }

        // Verify it's an image content type
        if (!document.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Non-image document {DocumentId} requested via image proxy", documentId);
            return BadRequest(new { error = "Document is not an image" });
        }

        var downloadUrl = await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);

        return Redirect(downloadUrl);
    }
}
