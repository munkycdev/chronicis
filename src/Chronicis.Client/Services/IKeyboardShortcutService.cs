namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating keyboard shortcut events across components.
/// Used primarily for Ctrl+S save functionality.
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Fired when Ctrl+S is pressed and the current article should be saved.
    /// </summary>
    event Action? OnSaveRequested;

    /// <summary>
    /// Triggers the save request event.
    /// </summary>
    void RequestSave();
}
