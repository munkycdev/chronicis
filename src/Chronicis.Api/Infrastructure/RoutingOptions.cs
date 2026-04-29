namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Configuration options for URL routing.
/// </summary>
public class RoutingOptions
{
    /// <summary>
    /// Slugs that are globally reserved and cannot be used for worlds, campaigns, arcs, sessions, or maps.
    /// Matched case-insensitively.
    /// </summary>
    public HashSet<string> ReservedSlugs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
