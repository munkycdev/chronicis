namespace Chronicis.Shared.DTOs;

/// <summary>
/// User profile information returned to the client.
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool HasCompletedOnboarding { get; set; }
}

/// <summary>
/// Request to complete the onboarding flow.
/// </summary>
public class CompleteOnboardingDto
{
    // Future: Could include preferences set during onboarding
    // For now, just marks onboarding as complete
}
