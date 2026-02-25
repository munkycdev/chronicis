namespace Chronicis.Client.Services;

/// <summary>
/// Coordinates mutually-exclusive right-side drawers.
/// </summary>
public interface IDrawerCoordinator
{
    /// <summary>
    /// Gets the currently active drawer.
    /// </summary>
    DrawerType Current { get; }

    /// <summary>
    /// Gets or sets whether the tutorial drawer is forced open.
    /// When enabled, the tutorial drawer is opened immediately and cannot be closed.
    /// </summary>
    bool IsForcedOpen { get; set; }

    /// <summary>
    /// Fired when drawer state changes.
    /// </summary>
    event Action? OnChanged;

    /// <summary>
    /// Opens the specified drawer and closes any other drawer.
    /// </summary>
    void Open(DrawerType type);

    /// <summary>
    /// Closes the current drawer unless prevented by forced tutorial mode.
    /// </summary>
    void Close();

    /// <summary>
    /// Toggles the specified drawer, respecting forced tutorial mode.
    /// </summary>
    void Toggle(DrawerType type);
}
