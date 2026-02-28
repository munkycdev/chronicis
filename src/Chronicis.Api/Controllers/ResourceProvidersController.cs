using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Controller for managing resource providers and their world associations.
/// </summary>
[ApiController]
[Route("api/worlds/{worldId}/resource-providers")]
[Authorize]
public class ResourceProvidersController : ControllerBase
{
    private readonly IResourceProviderService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ResourceProvidersController> _logger;

    public ResourceProvidersController(
        IResourceProviderService service,
        ICurrentUserService currentUserService,
        ILogger<ResourceProvidersController> logger)
    {
        _service = service;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all resource providers with their enabled status for a world.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <returns>List of providers with enabled status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorldResourceProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorldProviders(Guid worldId)
    {
        try
        {
            var user = await _currentUserService.GetRequiredUserAsync();
            var providers = await _service.GetWorldProvidersAsync(worldId, user.Id);

            var dtos = providers.Select(p => new WorldResourceProviderDto
            {
                Provider = new ResourceProviderDto
                {
                    Code = p.Provider.Code,
                    Name = p.Provider.Name,
                    Description = p.Provider.Description,
                    DocumentationLink = p.Provider.DocumentationLink,
                    License = p.Provider.License
                },
                IsEnabled = p.IsEnabled,
                LookupKey = p.LookupKey
            }).ToList();

            return Ok(dtos);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "World {WorldId} not found", worldId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to world {WorldId}", worldId);
            return Forbid();
        }
    }

    /// <summary>
    /// Enables or disables a resource provider for a world.
    /// Only the world owner can modify provider settings.
    /// </summary>
    /// <param name="worldId">The world ID</param>
    /// <param name="providerCode">The provider code</param>
    /// <param name="request">Toggle request</param>
    /// <returns>Success result</returns>
    [HttpPost("{providerCode}/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleProvider(
        Guid worldId,
        string providerCode,
        [FromBody] ToggleResourceProviderRequestDto request)
    {
        try
        {
            var user = await _currentUserService.GetRequiredUserAsync();
            await _service.SetProviderEnabledAsync(worldId, providerCode, request.Enabled, user.Id, request.LookupKey);

            return Ok(new { message = $"Provider {providerCode} {(request.Enabled ? "enabled" : "disabled")} successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogErrorSanitized(ex, "World {WorldId} or provider {ProviderCode} not found", worldId, providerCode);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to modify world {WorldId}", worldId);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid provider configuration for world {WorldId} and provider {ProviderCode}", worldId, providerCode);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid provider lookup key for world {WorldId} and provider {ProviderCode}", worldId, providerCode);
            return BadRequest(new { message = ex.Message });
        }
    }
}
