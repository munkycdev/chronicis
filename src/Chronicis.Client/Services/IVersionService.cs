namespace Chronicis.Client.Services;

/// <summary>
/// Represents the build version information stamped into wwwroot/version.json at CI time.
/// </summary>
public class BuildInfo
{
    /// <summary>Full version string, e.g. "3.0.142".</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The CI build/run number component, e.g. "142".</summary>
    public string BuildNumber { get; set; } = string.Empty;

    /// <summary>Short git SHA, e.g. "a1b2c3d". "local" when running outside CI.</summary>
    public string Sha { get; set; } = string.Empty;

    /// <summary>ISO-8601 UTC timestamp of the build.</summary>
    public string BuildDate { get; set; } = string.Empty;
}

/// <summary>
/// Provides the client build version resolved from wwwroot/version.json.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Returns the build information. Fetches once on first call; cached thereafter.
    /// </summary>
    Task<BuildInfo> GetBuildInfoAsync();
}
