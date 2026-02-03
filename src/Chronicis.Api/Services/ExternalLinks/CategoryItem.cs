namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Represents an indexed item within a category.
/// Used for efficient lookup and search without scanning blob storage.
/// </summary>
/// <param name="Id">Unique identifier in format "category/slug" (e.g., "spells/fireball").</param>
/// <param name="Title">Display title from fields.name or prettified slug.</param>
/// <param name="BlobName">Full blob path for content retrieval (e.g., "2014/spells/srd-2014_fireball.json").</param>
/// <param name="Pk">Optional primary key from JSON for debugging.</param>
public record CategoryItem(
    string Id,
    string Title,
    string BlobName,
    string? Pk = null
);
