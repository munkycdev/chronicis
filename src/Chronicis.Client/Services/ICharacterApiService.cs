using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for character claiming operations.
/// </summary>
public interface ICharacterApiService
{
    /// <summary>
    /// Gets all characters claimed by the current user.
    /// </summary>
    Task<List<ClaimedCharacterDto>> GetClaimedCharactersAsync();

    /// <summary>
    /// Claims a character for the current user.
    /// </summary>
    Task<bool> ClaimCharacterAsync(Guid characterId);

    /// <summary>
    /// Unclaims a character (removes the current user's claim).
    /// </summary>
    Task<bool> UnclaimCharacterAsync(Guid characterId);

    /// <summary>
    /// Gets the claim status of a character.
    /// </summary>
    Task<CharacterClaimStatusDto?> GetClaimStatusAsync(Guid characterId);
}

/// <summary>
/// Status of a character's claim.
/// </summary>
[ExcludeFromCodeCoverage]
public class CharacterClaimStatusDto
{
    public Guid CharacterId { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsClaimedByMe { get; set; }
    public string? ClaimedByName { get; set; }
}
