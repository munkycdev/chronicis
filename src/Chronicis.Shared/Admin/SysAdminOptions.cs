namespace Chronicis.Shared.Admin;

/// <summary>
/// Configuration for system administrator identity.
/// Populated from the "SysAdmin" section of appsettings.json on both API and Client.
/// </summary>
public class SysAdminOptions
{
    /// <summary>
    /// Auth0 user IDs (sub claim) that are considered system administrators.
    /// </summary>
    public IReadOnlyList<string> Auth0UserIds { get; init; } = [];

    /// <summary>
    /// Email addresses that are considered system administrators.
    /// </summary>
    public IReadOnlyList<string> Emails { get; init; } = [];
}
