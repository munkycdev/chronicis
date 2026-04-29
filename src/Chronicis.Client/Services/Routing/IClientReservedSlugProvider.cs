namespace Chronicis.Client.Services.Routing;

/// <summary>
/// Provides the client-side reserved root-slug list, sourced from Routing:ReservedSlugs config.
/// Used by PathResolver to short-circuit before calling the API.
/// </summary>
public interface IClientReservedSlugProvider
{
    /// <summary>Returns true if the slug is in the reserved list (case-insensitive).</summary>
    bool IsReserved(string slug);
}
