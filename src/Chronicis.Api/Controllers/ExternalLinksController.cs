using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for External Link operations (SRD content, etc.)
/// </summary>
[ApiController]
[Route("external-links")]
// [Authorize] // Temporarily disabled for testing
public class ExternalLinksController : ControllerBase
{
    private readonly IExternalLinkService _externalLinkService;
    private readonly ILogger<ExternalLinksController> _logger;

    public ExternalLinksController(
        IExternalLinkService externalLinkService,
        ILogger<ExternalLinksController> logger)
    {
        _externalLinkService = externalLinkService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/external-links/suggestions?worldId={worldId}&amp;source={source}&amp;query={query}
    /// Get external link suggestions for autocomplete.
    /// Filters to only include providers enabled for the specified world.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<ExternalLinkSuggestionDto>>> GetSuggestions(
        [FromQuery] Guid? worldId,
        [FromQuery] string? source,
        [FromQuery] string? query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Ok(new List<ExternalLinkSuggestionDto>());
        }

        _logger.LogDebugSanitized(
            "Getting external link suggestions for world {WorldId}, source '{Source}' with query '{Query}'",
            worldId, source, query);

        var suggestions = await _externalLinkService.GetSuggestionsAsync(worldId, source, query ?? "", ct);

        var dtos = suggestions.Select(s => new ExternalLinkSuggestionDto
        {
            Source = s.Source,
            Id = s.Id,
            Title = s.Title,
            Subtitle = s.Subtitle,
            Category = s.Category,
            Icon = s.Icon,
            Href = s.Href
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// GET /api/external-links/content?source={source}&amp;id={id}
    /// Get external link content for display.
    /// </summary>
    [HttpGet("content")]
    public async Task<ActionResult<ExternalLinkContentDto>> GetContent(
        [FromQuery] string? source,
        [FromQuery] string? id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ExternalLinkErrorDto { Message = "Source and id are required" });
        }

        _logger.LogDebugSanitized(
            "Getting external link content for source '{Source}' with id '{Id}'",
            source, id);

        var content = await _externalLinkService.GetContentAsync(source, id, ct);

        if (content == null)
        {
            return NotFound(new ExternalLinkErrorDto { Message = "Content not found" });
        }

        var dto = new ExternalLinkContentDto
        {
            Source = content.Source,
            Id = content.Id,
            Title = content.Title,
            Kind = content.Kind,
            Markdown = content.Markdown,
            Attribution = content.Attribution,
            ExternalUrl = content.ExternalUrl,
            JsonData = content.JsonData
        };

        return Ok(dto);
    }
}
