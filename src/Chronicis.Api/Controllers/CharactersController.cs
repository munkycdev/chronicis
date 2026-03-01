using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Character claiming operations.
/// </summary>
[ApiController]
[Route("characters")]
[Authorize]
public class CharactersController : ControllerBase
{
    private readonly ICharacterClaimService _characterClaimService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CharactersController> _logger;

    public CharactersController(
        ICharacterClaimService characterClaimService,
        ICurrentUserService currentUserService,
        ILogger<CharactersController> logger)
    {
        _characterClaimService = characterClaimService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/characters/claimed - Get all characters claimed by the current user.
    /// </summary>
    [HttpGet("claimed")]
    public async Task<ActionResult<List<ClaimedCharacterDto>>> GetClaimedCharacters()
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting claimed characters for user {UserId}", user.Id);
        var claimedCharacters = await _characterClaimService.GetClaimedCharactersAsync(user.Id);
        return Ok(claimedCharacters);
    }

    /// <summary>
    /// GET /api/characters/{id}/claim - Get the claim status of a character.
    /// </summary>
    [HttpGet("{id:guid}/claim")]
    public async Task<ActionResult<CharacterClaimStatusDto>> GetClaimStatus(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("Getting claim status for character {CharacterId}", id);

        var result = await _characterClaimService.GetClaimStatusAsync(id);
        if (!result.Found)
        {
            return NotFound(new { error = "Character not found" });
        }

        var status = new CharacterClaimStatusDto
        {
            CharacterId = id,
            IsClaimed = result.PlayerId.HasValue,
            IsClaimedByMe = result.PlayerId == user.Id,
            ClaimedByName = result.PlayerName
        };

        return Ok(status);
    }

    /// <summary>
    /// POST /api/characters/{id}/claim - Claim a character for the current user.
    /// </summary>
    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> ClaimCharacter(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("User {UserId} claiming character {CharacterId}", user.Id, id);
        var result = await _characterClaimService.ClaimCharacterAsync(id, user.Id);
        return result.Status switch
        {
            ServiceStatus.NotFound => NotFound(new { error = result.ErrorMessage }),
            ServiceStatus.Conflict => Conflict(new { error = result.ErrorMessage }),
            _ => NoContent()
        };
    }

    /// <summary>
    /// DELETE /api/characters/{id}/claim - Unclaim a character (remove current user's claim).
    /// </summary>
    [HttpDelete("{id:guid}/claim")]
    public async Task<IActionResult> UnclaimCharacter(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogDebug("User {UserId} unclaiming character {CharacterId}", user.Id, id);
        var result = await _characterClaimService.UnclaimCharacterAsync(id, user.Id);
        return result.Status == ServiceStatus.NotFound
            ? NotFound(new { error = result.ErrorMessage })
            : NoContent();
    }
}

/// <summary>
/// Status of a character's claim.
/// </summary>
public class CharacterClaimStatusDto
{
    public Guid CharacterId { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsClaimedByMe { get; set; }
    public string? ClaimedByName { get; set; }
}
