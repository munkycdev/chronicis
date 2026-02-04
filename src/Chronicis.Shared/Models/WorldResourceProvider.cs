namespace Chronicis.Shared.Models;

/// <summary>
/// Junction table tracking which resource providers are enabled for each world.
/// Includes audit trail of when and by whom providers were enabled.
/// </summary>
public class WorldResourceProvider
{
    /// <summary>
    /// The world this provider association belongs to
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// The resource provider code
    /// </summary>
    public string ResourceProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is currently enabled for the world
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// When this provider was last enabled/disabled
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// User ID who last enabled/disabled this provider
    /// </summary>
    public Guid ModifiedByUserId { get; set; }

    // Navigation properties
    public World World { get; set; } = null!;
    public ResourceProvider ResourceProvider { get; set; } = null!;
}
