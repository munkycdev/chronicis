using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// DTO for resource provider information.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResourceProviderDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentationLink { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
}

/// <summary>
/// DTO for resource provider with enabled status for a specific world.
/// </summary>
[ExcludeFromCodeCoverage]
public class WorldResourceProviderDto
{
    public ResourceProviderDto Provider { get; set; } = new();
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request to toggle a resource provider for a world.
/// </summary>
[ExcludeFromCodeCoverage]
public class ToggleResourceProviderRequestDto
{
    public bool Enabled { get; set; }
}
