using Chronicis.Shared.Admin;

namespace Chronicis.Client.Services;

/// <summary>
/// System admin authorization service.
/// Delegates to <see cref="ISysAdminChecker"/> which reads from the "SysAdmin"
/// configuration section, eliminating hardcoded identity sets.
/// </summary>
public class AdminAuthService : IAdminAuthService
{
    private readonly IAuthService _authService;
    private readonly ISysAdminChecker _sysAdminChecker;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(
        IAuthService authService,
        ISysAdminChecker sysAdminChecker,
        ILogger<AdminAuthService> logger)
    {
        _authService = authService;
        _sysAdminChecker = sysAdminChecker;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSysAdminAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
        {
            _logger.LogDebug("IsSysAdminAsync: no current user, returning false");
            return false;
        }

        return _sysAdminChecker.IsSysAdmin(user.Auth0UserId, user.Email);
    }
}
