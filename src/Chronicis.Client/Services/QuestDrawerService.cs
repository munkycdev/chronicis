namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating quest drawer toggle events.
/// Wraps the shared drawer coordinator and preserves the existing event-based API.
/// </summary>
public class QuestDrawerService : IQuestDrawerService, IDisposable
{
    private readonly IDrawerCoordinator _drawerCoordinator;
    private bool _isOpen;
    private bool _disposed;

    public event Action? OnOpen;
    public event Action? OnClose;

    public bool IsOpen => _isOpen;

    public QuestDrawerService(IDrawerCoordinator drawerCoordinator)
    {
        _drawerCoordinator = drawerCoordinator;
        _isOpen = _drawerCoordinator.Current == DrawerType.Quests;
        _drawerCoordinator.OnChanged += OnDrawerCoordinatorChanged;
    }

    public void Open()
    {
        if (_disposed)
            return;

        _drawerCoordinator.Open(DrawerType.Quests);
    }

    public void Close()
    {
        if (_disposed || !_isOpen)
            return;

        _drawerCoordinator.Close();
    }

    public void Toggle()
    {
        if (_disposed)
            return;

        _drawerCoordinator.Toggle(DrawerType.Quests);
    }

    private void OnDrawerCoordinatorChanged()
    {
        if (_disposed)
        {
            return;
        }

        var isOpenNow = _drawerCoordinator.Current == DrawerType.Quests;
        if (_isOpen == isOpenNow)
        {
            return;
        }

        _isOpen = isOpenNow;
        if (_isOpen)
        {
            OnOpen?.Invoke();
        }
        else
        {
            OnClose?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _drawerCoordinator.OnChanged -= OnDrawerCoordinatorChanged;
        OnOpen = null;
        OnClose = null;

        GC.SuppressFinalize(this);
    }
}
