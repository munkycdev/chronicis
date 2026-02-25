using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// SysAdmin-only API endpoints for tutorial page mappings.
/// </summary>
[ApiController]
[Route("sysadmin/tutorials")]
[Authorize]
public class SysAdminTutorialsController : ControllerBase
{
    private readonly ITutorialService _tutorialService;
    private readonly ILogger<SysAdminTutorialsController> _logger;

    public SysAdminTutorialsController(
        ITutorialService tutorialService,
        ILogger<SysAdminTutorialsController> logger)
    {
        _tutorialService = tutorialService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/sysadmin/tutorials - Lists all tutorial mappings.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TutorialMappingDto>>> GetMappings()
    {
        try
        {
            var mappings = await _tutorialService.GetMappingsAsync();
            return Ok(mappings);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to list tutorial mappings");
            return Forbid();
        }
    }

    /// <summary>
    /// POST /api/sysadmin/tutorials - Creates a tutorial mapping (and optionally a tutorial article).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TutorialMappingDto>> CreateMapping([FromBody] TutorialMappingCreateDto dto)
    {
        try
        {
            var mapping = await _tutorialService.CreateMappingAsync(dto);
            return CreatedAtAction(nameof(GetMappings), new { id = mapping.Id }, mapping);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to create tutorial mapping");
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    /// <summary>
    /// PUT /api/sysadmin/tutorials/{id} - Updates a tutorial mapping.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TutorialMappingDto>> UpdateMapping(Guid id, [FromBody] TutorialMappingUpdateDto dto)
    {
        try
        {
            var mapping = await _tutorialService.UpdateMappingAsync(id, dto);
            if (mapping == null)
            {
                return NotFound(new { error = "Tutorial mapping not found" });
            }

            return Ok(mapping);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to update tutorial mapping {MappingId}", id);
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    /// <summary>
    /// DELETE /api/sysadmin/tutorials/{id} - Deletes a tutorial mapping.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMapping(Guid id)
    {
        try
        {
            var deleted = await _tutorialService.DeleteMappingAsync(id);
            return deleted ? NoContent() : NotFound(new { error = "Tutorial mapping not found" });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized attempt to delete tutorial mapping {MappingId}", id);
            return Forbid();
        }
    }

    private ObjectResult MapInvalidOperation(InvalidOperationException ex)
    {
        if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = ex.Message });
        }

        return BadRequest(new { error = ex.Message });
    }
}
