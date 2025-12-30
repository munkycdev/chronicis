using System.Net;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for character claiming operations.
/// </summary>
public class CharacterFunctions
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<CharacterFunctions> _logger;

    public CharacterFunctions(ChronicisDbContext context, ILogger<CharacterFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all characters claimed by the current user.
    /// </summary>
    [Function("GetClaimedCharacters")]
    public async Task<HttpResponseData> GetClaimedCharacters(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "characters/claimed")] HttpRequestData req,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var claimedCharacters = await _context.Articles
            .Where(a => a.PlayerId == user.Id && a.Type == ArticleType.Character)
            .Include(a => a.World)
            .Select(a => new ClaimedCharacterDto
            {
                Id = a.Id,
                Title = a.Title,
                IconEmoji = a.IconEmoji,
                WorldId = a.WorldId ?? Guid.Empty,
                WorldName = a.World != null ? a.World.Name : "Unknown",
                ModifiedAt = a.ModifiedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(claimedCharacters);
        return response;
    }

    /// <summary>
    /// Claims a character for the current user.
    /// </summary>
    [Function("ClaimCharacter")]
    public async Task<HttpResponseData> ClaimCharacter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "characters/{characterId}/claim")] HttpRequestData req,
        Guid characterId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == characterId);

        if (article == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Character not found");
            return notFound;
        }

        // Verify it's a Character type
        if (article.Type != ArticleType.Character)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Only Character articles can be claimed");
            return badRequest;
        }

        // Check if already claimed by someone else
        if (article.PlayerId.HasValue && article.PlayerId != user.Id)
        {
            var conflict = req.CreateResponse(HttpStatusCode.Conflict);
            await conflict.WriteStringAsync("This character is already claimed by another user");
            return conflict;
        }

        // Claim the character
        article.PlayerId = user.Id;
        article.ModifiedAt = DateTime.UtcNow;
        article.LastModifiedBy = user.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} claimed character {CharacterId}", user.Id, characterId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { success = true, characterId });
        return response;
    }

    /// <summary>
    /// Unclaims a character (removes the current user's claim).
    /// </summary>
    [Function("UnclaimCharacter")]
    public async Task<HttpResponseData> UnclaimCharacter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "characters/{characterId}/claim")] HttpRequestData req,
        Guid characterId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == characterId);

        if (article == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Character not found");
            return notFound;
        }

        // Verify it's claimed by the current user
        if (article.PlayerId != user.Id)
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteStringAsync("You can only unclaim your own characters");
            return forbidden;
        }

        // Unclaim the character
        article.PlayerId = null;
        article.ModifiedAt = DateTime.UtcNow;
        article.LastModifiedBy = user.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unclaimed character {CharacterId}", user.Id, characterId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { success = true, characterId });
        return response;
    }

    /// <summary>
    /// Gets the claim status of a character.
    /// </summary>
    [Function("GetCharacterClaimStatus")]
    public async Task<HttpResponseData> GetCharacterClaimStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "characters/{characterId}/claim")] HttpRequestData req,
        Guid characterId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();

        var article = await _context.Articles
            .Where(a => a.Id == characterId)
            .Select(a => new
            {
                a.Id,
                a.PlayerId,
                a.Type,
                PlayerName = a.Player != null ? a.Player.DisplayName : null
            })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync("Character not found");
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            characterId = article.Id,
            isClaimed = article.PlayerId.HasValue,
            isClaimedByMe = article.PlayerId == user.Id,
            claimedByName = article.PlayerName
        });
        return response;
    }
}
