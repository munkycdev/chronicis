using Chronicis.Shared.Admin;

namespace Chronicis.Client.Services;

/// <summary>
/// Determines whether the current user has system administrator privileges.
/// Extends the shared <see cref="ISysAdminAuthService"/> contract so the same
/// interface is used on both API and Client.
/// </summary>
public interface IAdminAuthService : ISysAdminAuthService
{
}
