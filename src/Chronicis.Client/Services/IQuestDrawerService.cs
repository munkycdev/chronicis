namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating quest drawer toggle events and state across components.
/// Used for keyboard shortcut handling (Ctrl+Q) and mutual exclusivity with metadata drawer.
/// </summary>
public interface IQuestDrawerService
{
    /// <summary>
    /// Fired when the quest drawer should be opened.
    /// </summary>
    event Action? OnOpen;
    
    /// <summary>
    /// Fired when the quest drawer should be closed.
    /// </summary>
    event Action? OnClose;
    
    /// <summary>
    /// Triggers the open event.
    /// </summary>
    void Open();
    
    /// <summary>
    /// Triggers the close event.
    /// </summary>
    void Close();
    
    /// <summary>
    /// Toggles between open and closed states.
    /// </summary>
    void Toggle();
    
    /// <summary>
    /// Gets whether the drawer is currently open.
    /// </summary>
    bool IsOpen { get; }
}
