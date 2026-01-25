namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating metadata drawer toggle events across components.
/// Used primarily for keyboard shortcut handling (Ctrl+M).
/// </summary>
public interface IMetadataDrawerService
{
    /// <summary>
    /// Fired when the metadata drawer should be toggled.
    /// </summary>
    event Action? OnToggle;
    
    /// <summary>
    /// Triggers the toggle event.
    /// </summary>
    void Toggle();
}
