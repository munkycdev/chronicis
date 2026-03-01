using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class CharacterClaimService : ICharacterClaimService
{
    private readonly ChronicisDbContext _context;

    public CharacterClaimService(ChronicisDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClaimedCharacterDto>> GetClaimedCharactersAsync(Guid userId)
    {
        return await _context.Articles
            .Where(a => a.PlayerId == userId && a.Type == ArticleType.Character)
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
    }

    public async Task<(bool Found, Guid? PlayerId, string? PlayerName)> GetClaimStatusAsync(Guid characterId)
    {
        var article = await _context.Articles
            .Where(a => a.Id == characterId && a.Type == ArticleType.Character)
            .Select(a => new
            {
                a.PlayerId,
                PlayerName = a.Player != null ? a.Player.DisplayName : null
            })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return (false, null, null);
        }

        return (true, article.PlayerId, article.PlayerName);
    }

    public async Task<ServiceResult<bool>> ClaimCharacterAsync(Guid characterId, Guid userId)
    {
        var article = await _context.Articles
            .Where(a => a.Id == characterId && a.Type == ArticleType.Character)
            .Where(a => a.WorldId.HasValue && a.World!.Members.Any(m => m.UserId == userId))
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return ServiceResult<bool>.NotFound("Character not found or access denied");
        }

        if (article.PlayerId.HasValue && article.PlayerId != userId)
        {
            return ServiceResult<bool>.Conflict("Character is already claimed by another player");
        }

        article.PlayerId = userId;
        article.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> UnclaimCharacterAsync(Guid characterId, Guid userId)
    {
        var article = await _context.Articles
            .Where(a => a.Id == characterId && a.Type == ArticleType.Character)
            .Where(a => a.PlayerId == userId)
            .FirstOrDefaultAsync();

        if (article == null)
        {
            return ServiceResult<bool>.NotFound("Character not found or not claimed by you");
        }

        article.PlayerId = null;
        article.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }
}

