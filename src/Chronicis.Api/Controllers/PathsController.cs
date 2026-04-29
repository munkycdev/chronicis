using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services.Routing;
using Chronicis.Shared.Routing;
using Chronicis.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Unified slug-path resolution endpoint. Anonymous-accessible; gating is handled by IReadAccessPolicyService.
/// </summary>
[ApiController]
[Route("paths")]
[AllowAnonymous]
public sealed class PathsController : ControllerBase
{
    private static readonly HashSet<string> KeywordSegments =
        new(StringComparer.OrdinalIgnoreCase) { "wiki", "maps" };

    private readonly ISlugPathResolver _resolver;
    private readonly ICurrentUserService _currentUserService;

    public PathsController(ISlugPathResolver resolver, ICurrentUserService currentUserService)
    {
        _resolver = resolver;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// GET /paths/resolve/{*path}
    /// Resolves a URL path into a typed entity identity.
    /// Returns 200 with SlugPathResolution, 404 when not found, 400 when a segment is invalid.
    /// </summary>
    [HttpGet("resolve/{*path}")]
    public async Task<ActionResult<SlugPathResolution>> Resolve(
        string path,
        CancellationToken cancellationToken)
    {
        var segments = (path ?? string.Empty)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToList();

        if (segments.Count == 0)
            return NotFound();

        // Validate each segment — keywords are exempt from slug validation
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (!KeywordSegments.Contains(seg) && !SlugGenerator.IsValidSlug(seg))
                return BadRequest();
        }

        var currentUserId = _currentUserService.IsAuthenticated
            ? (Guid?)((await _currentUserService.GetCurrentUserAsync())?.Id)
            : null;

        var resolution = await _resolver.ResolveAsync(segments, currentUserId, cancellationToken);

        return resolution == null ? NotFound() : Ok(resolution);
    }
}
