namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating keyboard shortcut events across components.
/// </summary>
public class KeyboardShortcutService : IKeyboardShortcutService
{
    public event Action? OnSaveRequested;
    
    public void RequestSave() => OnSaveRequested?.Invoke();
}