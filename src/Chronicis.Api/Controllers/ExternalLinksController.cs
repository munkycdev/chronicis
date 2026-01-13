using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for External Link operations (SRD content, etc.)
/// </summary>
[ApiController]
[Route("external-links")]
[Authorize]
public class ExternalLinksController : ControllerBase
{
    private readonly ExternalLinkSuggestionService _suggestionService;
    private readonly ExternalLinkContentService _contentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExternalLinksController> _logger;

    public ExternalLinksController(
        ExternalLinkSuggestionService suggestionService,
        ExternalLinkContentService contentService,
        ICurrentUserService currentUserService,
        ILogger<ExternalLinksController> logger)
    {
        _suggestionService = suggestionService;
        _contentService = contentService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/external-links/suggestions?source={source}&query={query}
    /// Get external link suggestions for autocomplete.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<ExternalLinkSuggestionDto>>> GetSuggestions(
        [FromQuery] string source,
        [FromQuery] string query,
        CancellationToken ct)
    {
        // Ensure user is authenticated
        await _currentUserService.GetRequiredUserAsync();

        if (string.IsNullOrWhiteSpace(source))
        {
            return Ok(new List<ExternalLinkSuggestionDto>());
        }

        _logger.LogInformation("Getting external link suggestions for source '{Source}' with query '{Query}'", source, query);

        var suggestions = await _suggestionService.GetSuggestionsAsync(source, query ?? "", ct);

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
    /// GET /api/external-links/content?source={source}&id={id}
    /// Get external link content for display.
    /// </summary>
    [HttpGet("content")]
    public async Task<ActionResult<ExternalLinkContentDto>> GetContent(
        [FromQuery] string source,
        [FromQuery] string id,
        CancellationToken ct)
    {
        // Ensure user is authenticated
        await _currentUserService.GetRequiredUserAsync();

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ExternalLinkErrorDto { Message = "Source and id are required" });
        }

        _logger.LogInformation("Getting external link content for source '{Source}' with id '{Id}'", source, id);

        var content = await _contentService.GetContentAsync(source, id, ct);

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
            ExternalUrl = content.ExternalUrl
        };

        return Ok(dto);
    }
}
