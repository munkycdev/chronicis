using System.Security.Claims;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Implementation of ICurrentUserService that resolves the user from HTTP context claims.
/// This service is scoped per-request and caches the user lookup for the request lifetime.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private User? _cachedUser;
    private bool _userLookedUp;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IUserService userService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? GetAuth0UserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !IsAuthenticated)
            return null;

        // Auth0 puts the user ID in the 'sub' claim (NameIdentifier)
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        // Return cached user if already looked up this request
        if (_userLookedUp)
            return _cachedUser;

        _userLookedUp = true;

        var auth0UserId = GetAuth0UserId();
        if (string.IsNullOrEmpty(auth0UserId))
            return null;

        // Extract additional claims for user creation/update
        var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
        
        const string customNamespace = "https://chronicis.app";
        
        var email = claimsPrincipal?.FindFirst($"{customNamespace}/email")?.Value
                   ?? claimsPrincipal?.FindFirst(ClaimTypes.Email)?.Value
                   ?? claimsPrincipal?.FindFirst("email")?.Value
                   ?? "";

        var displayName = claimsPrincipal?.FindFirst($"{customNamespace}/name")?.Value
                         ?? claimsPrincipal?.FindFirst(ClaimTypes.Name)?.Value
                         ?? claimsPrincipal?.FindFirst("name")?.Value
                         ?? claimsPrincipal?.FindFirst("nickname")?.Value
                         ?? claimsPrincipal?.FindFirst("preferred_username")?.Value
                         ?? claimsPrincipal?.FindFirst("given_name")?.Value
                         ?? ExtractNameFromEmail(email)
                         ?? "Unknown User";

        var avatarUrl = claimsPrincipal?.FindFirst($"{customNamespace}/picture")?.Value
                       ?? claimsPrincipal?.FindFirst("picture")?.Value;

        // Get or create the user in the database
        _cachedUser = await _userService.GetOrCreateUserAsync(
            auth0UserId,
            email,
            displayName,
            avatarUrl);

        return _cachedUser;
    }

    public async Task<User> GetRequiredUserAsync()
    {
        var user = await GetCurrentUserAsync();
        return user ?? throw new InvalidOperationException(
            "User not found. Ensure this endpoint requires authentication.");
    }

    /// <summary>
    /// Extracts a display name from an email address as a fallback.
    /// e.g., "john.doe@example.com" becomes "John Doe"
    /// </summary>
    private static string? ExtractNameFromEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return null;

        var localPart = email.Split('@')[0];
        
        // Replace common separators with spaces
        var name = localPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ');

        // Title case each word
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleCased = words.Select(w => 
            char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1).ToLower() : ""));

        var result = string.Join(" ", titleCased);
        
        // Don't return if it looks like gibberish (all numbers, too short, etc.)
        if (result.Length < 2 || result.All(c => char.IsDigit(c)))
            return null;

        return result;
    }
}
