using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Global Search operations.
/// </summary>
[ApiController]
[Route("search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchReadService _searchReadService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchReadService searchReadService,
        ICurrentUserService currentUserService,
        ILogger<SearchController> logger)
    {
        _searchReadService = searchReadService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/search?query={query}
    /// Searches across all article content the user has access to.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GlobalSearchResultsDto>> Search([FromQuery] string query)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebugSanitized("Searching for '{Query}' for user {UserId}", query, user.Id);
        var response = await _searchReadService.SearchAsync(query, user.Id);
        return Ok(response);
    }
}
