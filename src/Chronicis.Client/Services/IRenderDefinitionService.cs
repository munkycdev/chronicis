using Chronicis.Client.Models;

namespace Chronicis.Client.Services;

/// <summary>
/// Resolves render definitions for external link content by category path.
/// Walks from most-specific to least-specific, falling back to a default.
/// </summary>
public interface IRenderDefinitionService
{
    /// <summary>
    /// Resolves the best render definition for the given source and category path.
    /// Resolution order: source/full/category/path → source/parent → ... → source → _default.
    /// </summary>
    /// <param name="source">Provider key (e.g., "ros", "srd").</param>
    /// <param name="categoryPath">
    /// Category portion of the content ID (e.g., "bestiary/Cultural-Being").
    /// Can be null/empty for root-level content.
    /// </param>
    /// <returns>The resolved definition, or the built-in default if none found.</returns>
    Task<RenderDefinition> ResolveAsync(string source, string? categoryPath);
}
