using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents an external resource provider (e.g., SRD, Open5e API)
/// that can be enabled for worlds to access reference content.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResourceProvider
{
    /// <summary>
    /// Unique code identifier for the provider (e.g., "srd", "srd14", "srd24", "ros")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the provider
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what content this provider offers
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL to the provider's documentation
    /// </summary>
    public string DocumentationLink { get; set; } = string.Empty;

    /// <summary>
    /// URL to the provider's license information
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is active and available for worlds to enable
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this provider was added to the system
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    public ICollection<WorldResourceProvider> WorldResourceProviders { get; set; } = new List<WorldResourceProvider>();
}
