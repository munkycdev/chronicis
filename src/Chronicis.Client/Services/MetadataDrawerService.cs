namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating metadata drawer toggle events across components.
/// </summary>
public class MetadataDrawerService : IMetadataDrawerService
{
    public event Action? OnToggle;

    public void Toggle() => OnToggle?.Invoke();
}
