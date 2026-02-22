namespace Chronicis.Shared.Admin;

/// <summary>
/// Evaluates system administrator status by checking a user's Auth0 ID or email
/// against the configured <see cref="SysAdminOptions"/>.
/// </summary>
public class SysAdminChecker : ISysAdminChecker
{
    private readonly HashSet<string> _auth0UserIds;
    private readonly HashSet<string> _emails;

    public SysAdminChecker(SysAdminOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _auth0UserIds = new HashSet<string>(
            options.Auth0UserIds ?? [],
            StringComparer.OrdinalIgnoreCase);

        _emails = new HashSet<string>(
            options.Emails ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool IsSysAdmin(string auth0UserId, string? email)
    {
        if (!string.IsNullOrEmpty(auth0UserId) && _auth0UserIds.Contains(auth0UserId))
            return true;

        if (!string.IsNullOrEmpty(email) && _emails.Contains(email))
            return true;

        return false;
    }
}
