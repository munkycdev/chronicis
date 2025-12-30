using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for user profile operations.
/// </summary>
public interface IUserApiService
{
    /// <summary>
    /// Gets the current user's profile information.
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync();

    /// <summary>
    /// Marks the current user's onboarding as complete.
    /// </summary>
    Task<bool> CompleteOnboardingAsync();
}
