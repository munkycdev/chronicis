using Chronicis.Shared.Routing;

namespace Chronicis.Client.Services;

/// <summary>
/// Client service for the unified path-resolution endpoint.
/// </summary>
public interface IPathApiService
{
    /// <summary>
    /// Resolves a URL path (e.g. "my-world/my-campaign/arc-1") to a typed entity identity.
    /// Returns null when the path is not found or the entity is not accessible.
    /// </summary>
    Task<SlugPathResolution?> ResolveAsync(string path, CancellationToken cancellationToken = default);
}
