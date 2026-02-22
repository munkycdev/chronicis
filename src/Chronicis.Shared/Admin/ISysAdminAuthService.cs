namespace Chronicis.Shared.Admin;

/// <summary>
/// Shared contract for determining whether the currently authenticated user
/// is a system administrator. Implemented by both the API's CurrentUserService
/// and the client's AdminAuthService.
/// </summary>
public interface ISysAdminAuthService
{
    /// <summary>
    /// Returns true if the current authenticated user is a system administrator.
    /// </summary>
    Task<bool> IsSysAdminAsync();
}
