namespace Chronicis.Client.Services;

/// <summary>
/// System admin authorization service.
/// TODO: Replace hardcoded user check with role-based claims from Auth0.
/// </summary>
public class AdminAuthService : IAdminAuthService
{
    private readonly IAuthService _authService;
    private readonly ILogger<AdminAuthService> _logger;

    // Hardcoded sysadmin identifiers — extend to role-based in the future
    private static readonly HashSet<string> SysAdminEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "dave@chronicis.app"
    };

    private static readonly HashSet<string> SysAdminAuth0Ids = new(StringComparer.OrdinalIgnoreCase)
    {
        "oauth2|discord|992501439685460139"
    };

    public AdminAuthService(IAuthService authService, ILogger<AdminAuthService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<bool> IsSysAdminAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return false;

        return SysAdminEmails.Contains(user.Email)
            || SysAdminAuth0Ids.Contains(user.Auth0UserId);
    }
}
