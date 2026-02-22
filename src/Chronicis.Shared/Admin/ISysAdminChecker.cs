namespace Chronicis.Shared.Admin;

/// <summary>
/// Determines whether a given user identity has system administrator privileges.
/// Implementations evaluate against a configured list of Auth0 user IDs and emails.
/// </summary>
public interface ISysAdminChecker
{
    /// <summary>
    /// Returns true if the supplied Auth0 user ID or email matches a known sysadmin identity.
    /// </summary>
    /// <param name="auth0UserId">The Auth0 'sub' claim value for the user.</param>
    /// <param name="email">The email address for the user. May be null.</param>
    bool IsSysAdmin(string auth0UserId, string? email);
}
