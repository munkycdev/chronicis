using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Proxy endpoint for serving inline article images.
/// Resolves document IDs to fresh SAS download URLs via 302 redirect.
/// This avoids storing expiring SAS URLs in article HTML content.
/// </summary>
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly IImageAccessService _imageAccessService;
    private readonly ICurrentUserService _currentUserService;

    public ImagesController(
        IImageAccessService imageAccessService,
        ICurrentUserService currentUserService)
    {
        _imageAccessService = imageAccessService;
        _currentUserService = currentUserService;
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
        var result = await _imageAccessService.GetImageDownloadUrlAsync(documentId, user.Id);
        return result.Status switch
        {
            ServiceStatus.NotFound => NotFound(),
            ServiceStatus.Forbidden => Forbid(),
            ServiceStatus.ValidationError => BadRequest(new { error = result.ErrorMessage }),
            _ => Redirect(result.Value!)
        };
    }
}
