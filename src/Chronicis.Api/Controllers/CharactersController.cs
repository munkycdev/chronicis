using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Controllers;

/// <summary>
/// API endpoints for Character claiming operations.
/// </summary>
[ApiController]
[Route("api/characters")]
[Authorize]
public class CharactersController : ControllerBase
{
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CharactersController> _logger;

    public CharactersController(
        ChronicisDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CharactersController> logger)
    {
        _context = context;
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
        _logger.LogInformation("Getting claimed characters for user {UserId}", user.Id);

        var claimedCharacters = await _context.Articles
            .Where(a => a.PlayerId == user.Id && a.Type == ArticleType.Character)
            .Where(a => a.WorldId.HasValue)
            .Select(a => new ClaimedCharacterDto
            {
                Id = a.Id,
                Title = a.Title ?? "Unnamed Character",
                IconEmoji = a.IconEmoji,
                WorldId = a.WorldId!.Value,
                WorldName = a.World != null ? a.World.Name : "Unknown World",
                ModifiedAt = a.ModifiedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(claimedCharacters);
    }

    /// <summary>
    /// GET /api/characters/{id}/claim - Get the claim status of a character.
    /// </summary>
    [HttpGet("{id:guid}/claim")]
    public async Task<ActionResult<CharacterClaimStatusDto>> GetClaimStatus(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("Getting claim status for character {CharacterId}", id);

        var article = await _context.Articles
            .Where(a => a.Id == id && a.Type == ArticleType.Character)
            .Select(a => new
            {
                a.Id,
                a.PlayerId,
                PlayerName = a.Player != null ? a.Player.DisplayName : null
            })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Character not found" });
        }

        var status = new CharacterClaimStatusDto
        {
            CharacterId = article.Id,
            IsClaimed = article.PlayerId.HasValue,
            IsClaimedByMe = article.PlayerId == user.Id,
            ClaimedByName = article.PlayerName
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
        _logger.LogInformation("User {UserId} claiming character {CharacterId}", user.Id, id);

        // Get the character article and verify user has access to its world
        var article = await _context.Articles
            .Where(a => a.Id == id && a.Type == ArticleType.Character)
            .Where(a => a.WorldId.HasValue && a.World!.Members.Any(m => m.UserId == user.Id))
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Character not found or access denied" });
        }

        // Check if already claimed by someone else
        if (article.PlayerId.HasValue && article.PlayerId != user.Id)
        {
            return Conflict(new { error = "Character is already claimed by another player" });
        }

        // Claim the character
        article.PlayerId = user.Id;
        article.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} claimed by user {UserId}", id, user.Id);
        return NoContent();
    }

    /// <summary>
    /// DELETE /api/characters/{id}/claim - Unclaim a character (remove current user's claim).
    /// </summary>
    [HttpDelete("{id:guid}/claim")]
    public async Task<IActionResult> UnclaimCharacter(Guid id)
    {
        var user = await _currentUserService.GetRequiredUserAsync();
        _logger.LogInformation("User {UserId} unclaiming character {CharacterId}", user.Id, id);

        // Get the character article
        var article = await _context.Articles
            .Where(a => a.Id == id && a.Type == ArticleType.Character)
            .Where(a => a.PlayerId == user.Id) // Only allow unclaiming own characters
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return NotFound(new { error = "Character not found or not claimed by you" });
        }

        // Unclaim the character
        article.PlayerId = null;
        article.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} unclaimed by user {UserId}", id, user.Id);
        return NoContent();
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
