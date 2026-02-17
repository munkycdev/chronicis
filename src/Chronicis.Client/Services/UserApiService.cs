using System.Net.Http.Json;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Implementation of user profile API service.
/// </summary>
public class UserApiService : IUserApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<UserApiService> _logger;

    public UserApiService(HttpClient http, ILogger<UserApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<UserProfileDto>("users/me");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("User profile not found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile");
            return null;
        }
    }

    public async Task<bool> CompleteOnboardingAsync()
    {
        try
        {
            var response = await _http.PostAsync("users/me/complete-onboarding", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding");
            return false;
        }
    }
}
