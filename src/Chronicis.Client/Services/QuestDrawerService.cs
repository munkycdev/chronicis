namespace Chronicis.Client.Services;

/// <summary>
/// Service for coordinating quest drawer toggle events and mutual exclusivity with metadata drawer.
/// Ensures only one drawer is open at a time.
/// Implements IDisposable to properly clean up event subscriptions.
/// </summary>
public class QuestDrawerService : IQuestDrawerService, IDisposable
{
    private readonly IMetadataDrawerService _metadataDrawerService;
    private bool _isOpen;
    private bool _isProcessingMutualExclusivity;
    private bool _disposed;

    public event Action? OnOpen;
    public event Action? OnClose;

    public bool IsOpen => _isOpen;

    public QuestDrawerService(IMetadataDrawerService metadataDrawerService)
    {
        _metadataDrawerService = metadataDrawerService;
        
        // Subscribe to metadata drawer toggle to ensure mutual exclusivity
        _metadataDrawerService.OnToggle += OnMetadataDrawerToggled;
    }

    public void Open()
    {
        if (_disposed || _isOpen || _isProcessingMutualExclusivity)
            return;
        
        _isProcessingMutualExclusivity = true;
        try
        {
            _isOpen = true;
            OnOpen?.Invoke();
        }
        finally
        {
            _isProcessingMutualExclusivity = false;
        }
    }

    public void Close()
    {
        if (_disposed || !_isOpen)
            return;
        
        _isOpen = false;
        OnClose?.Invoke();
    }

    public void Toggle()
    {
        if (_disposed)
            return;
            
        if (_isOpen)
            Close();
        else
            Open();
    }

    private void OnMetadataDrawerToggled()
    {
        // If metadata drawer is toggled while quest drawer is open, close quest drawer
        // to ensure mutual exclusivity
        if (_isOpen && !_isProcessingMutualExclusivity && !_disposed)
        {
            _isOpen = false;
            OnClose?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
            
        _disposed = true;
        
        // Unsubscribe from metadata drawer events to prevent memory leaks
        _metadataDrawerService.OnToggle -= OnMetadataDrawerToggled;
        
        // Clear event handlers
        OnOpen = null;
        OnClose = null;
        
        GC.SuppressFinalize(this);
    }
}
