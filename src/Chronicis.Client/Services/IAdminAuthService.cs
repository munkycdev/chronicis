namespace Chronicis.Client.Services;

/// <summary>
/// Determines whether the current user has system administrator privileges.
/// Currently hardcoded to a specific user; will be extended to support
/// role-based admin access in the future.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Returns true if the current authenticated user is a system administrator.
    /// </summary>
    Task<bool> IsSysAdminAsync();
}
