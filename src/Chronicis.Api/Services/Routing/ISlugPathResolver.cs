using Chronicis.Shared.Routing;

namespace Chronicis.Api.Services.Routing;

public interface ISlugPathResolver
{
    /// <summary>
    /// Resolve a URL path (split into segments) into an entity identity.
    /// Returns null when the path does not map to any accessible entity.
    /// </summary>
    Task<SlugPathResolution?> ResolveAsync(
        IReadOnlyList<string> segments,
        Guid? currentUserId,
        CancellationToken cancellationToken = default);
}
