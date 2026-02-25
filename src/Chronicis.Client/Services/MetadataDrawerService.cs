namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating metadata drawer toggle events across components.
/// </summary>
public class MetadataDrawerService : IMetadataDrawerService
{
    private readonly IDrawerCoordinator _drawerCoordinator;

    public MetadataDrawerService(IDrawerCoordinator drawerCoordinator)
    {
        _drawerCoordinator = drawerCoordinator;
    }

    public event Action? OnToggle;

    public void Toggle()
    {
        _drawerCoordinator.Toggle(DrawerType.Metadata);
        OnToggle?.Invoke();
    }
}
