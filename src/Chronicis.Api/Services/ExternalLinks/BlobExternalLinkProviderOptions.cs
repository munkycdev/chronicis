namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Configuration options for a blob-backed external link provider (e.g., SRD 2014, SRD 2024).
/// </summary>
public record BlobExternalLinkProviderOptions
{
    /// <summary>
    /// Provider key used in API requests (e.g., "srd14", "srd24").
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Display name shown in UI and attribution (e.g., "SRD 2014", "SRD 2024").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Azure Storage connection string.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Blob container name (e.g., "chronicis-external-links").
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Root prefix path within container (e.g., "2014/", "2024/").
    /// Must end with a slash.
    /// </summary>
    public required string RootPrefix { get; init; }

    /// <summary>
    /// Maximum number of suggestions to return from a search.
    /// </summary>
    public int MaxSuggestions { get; init; } = 20;

    /// <summary>
    /// Number of items to return when query is "category/" with no search term.
    /// </summary>
    public int FirstNCategoryItems { get; init; } = 20;

    /// <summary>
    /// Cache TTL for categories list (in minutes).
    /// </summary>
    public int CategoriesCacheTtl { get; init; } = 30;

    /// <summary>
    /// Cache TTL for per-category item indexes (in minutes).
    /// </summary>
    public int CategoryIndexCacheTtl { get; init; } = 30;

    /// <summary>
    /// Cache TTL for rendered content (in minutes).
    /// </summary>
    public int ContentCacheTtl { get; init; } = 15;
}
