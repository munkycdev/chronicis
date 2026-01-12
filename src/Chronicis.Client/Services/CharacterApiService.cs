using System.Net.Http.Json;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Implementation of character claiming API service.
/// </summary>
public class CharacterApiService : ICharacterApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<CharacterApiService> _logger;

    public CharacterApiService(HttpClient http, ILogger<CharacterApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ClaimedCharacterDto>> GetClaimedCharactersAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ClaimedCharacterDto>>("characters/claimed");
            return result ?? new List<ClaimedCharacterDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching claimed characters");
            return new List<ClaimedCharacterDto>();
        }
    }

    public async Task<bool> ClaimCharacterAsync(Guid characterId)
    {
        try
        {
            var response = await _http.PostAsync($"characters/{characterId}/claim", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming character {CharacterId}", characterId);
            return false;
        }
    }

    public async Task<bool> UnclaimCharacterAsync(Guid characterId)
    {
        try
        {
            var response = await _http.DeleteAsync($"characters/{characterId}/claim");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unclaiming character {CharacterId}", characterId);
            return false;
        }
    }

    public async Task<CharacterClaimStatusDto?> GetClaimStatusAsync(Guid characterId)
    {
        try
        {
            return await _http.GetFromJsonAsync<CharacterClaimStatusDto>($"characters/{characterId}/claim");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim status for character {CharacterId}", characterId);
            return null;
        }
    }
}
