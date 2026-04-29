using Chronicis.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace Chronicis.Api.Services;

/// <summary>
/// Singleton that exposes the reserved slug set loaded from <c>Routing:ReservedSlugs</c> config.
/// </summary>
public sealed class ReservedSlugProvider : IReservedSlugProvider
{
    private readonly HashSet<string> _reserved;

    public ReservedSlugProvider(IOptions<RoutingOptions> options)
    {
        _reserved = new HashSet<string>(
            options.Value.ReservedSlugs ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsReserved(string slug) => _reserved.Contains(slug);

    public IReadOnlyCollection<string> All => _reserved;
}
