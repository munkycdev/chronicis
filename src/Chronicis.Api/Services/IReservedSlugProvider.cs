namespace Chronicis.Api.Services;

/// <summary>
/// Provides access to the configurable set of reserved URL slugs.
/// </summary>
public interface IReservedSlugProvider
{
    /// <summary>Returns true when <paramref name="slug"/> is a reserved word (case-insensitive).</summary>
    bool IsReserved(string slug);

    /// <summary>All reserved slugs in their canonical (lowercase) form.</summary>
    IReadOnlyCollection<string> All { get; }
}
