namespace Chronicis.Client.Services;

/// <summary>
/// Default implementation for coordinating mutually-exclusive right-side drawers.
/// </summary>
public class DrawerCoordinator : IDrawerCoordinator
{
    private DrawerType _current = DrawerType.None;
    private bool _isForcedOpen;

    public DrawerType Current => _current;

    public bool IsForcedOpen
    {
        get => _isForcedOpen;
        set
        {
            if (_isForcedOpen == value)
            {
                return;
            }

            _isForcedOpen = value;

            if (_isForcedOpen && _current != DrawerType.Tutorial)
            {
                _current = DrawerType.Tutorial;
            }

            OnChanged?.Invoke();
        }
    }

    public event Action? OnChanged;

    public void Open(DrawerType type)
    {
        if (type == DrawerType.None)
        {
            Close();
            return;
        }

        if (_isForcedOpen && type != DrawerType.Tutorial)
        {
            type = DrawerType.Tutorial;
        }

        if (_current == type)
        {
            return;
        }

        _current = type;
        OnChanged?.Invoke();
    }

    public void Close()
    {
        if (_isForcedOpen && _current == DrawerType.Tutorial)
        {
            return;
        }

        if (_current == DrawerType.None)
        {
            return;
        }

        _current = DrawerType.None;
        OnChanged?.Invoke();
    }

    public void Toggle(DrawerType type)
    {
        if (type == DrawerType.None)
        {
            Close();
            return;
        }

        if (_isForcedOpen && type != DrawerType.Tutorial)
        {
            Open(DrawerType.Tutorial);
            return;
        }

        if (_current == type)
        {
            if (_isForcedOpen && type == DrawerType.Tutorial)
            {
                return;
            }

            Close();
            return;
        }

        Open(type);
    }
}
