namespace Chronicis.Client.Services.Routing;

/// <summary>
/// Reads the reserved-slug list from the "Routing:ReservedSlugs" configuration section.
/// Missing config defaults to an empty set.
/// </summary>
public sealed class ClientReservedSlugProvider : IClientReservedSlugProvider
{
    private readonly HashSet<string> _slugs;

    public ClientReservedSlugProvider(IConfiguration configuration)
    {
        var raw = configuration.GetSection("Routing:ReservedSlugs").Get<string[]>() ?? [];
        _slugs = new HashSet<string>(raw, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsReserved(string slug) => _slugs.Contains(slug);
}
