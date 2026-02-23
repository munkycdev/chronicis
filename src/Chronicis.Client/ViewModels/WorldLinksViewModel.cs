using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel managing external link CRUD for a world's Resources tab.
/// </summary>
public sealed class WorldLinksViewModel : ViewModelBase
{
    private readonly IWorldApiService _worldApi;
    private readonly ITreeStateService _treeState;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<WorldLinksViewModel> _logger;

    private Guid _worldId;
    private List<WorldLinkDto> _links = new();
    private bool _isAddingLink;
    private bool _isSavingLink;
    private string _newLinkTitle = string.Empty;
    private string _newLinkUrl = string.Empty;
    private string _newLinkDescription = string.Empty;
    private Guid? _editingLinkId;
    private string _editLinkTitle = string.Empty;
    private string _editLinkUrl = string.Empty;
    private string _editLinkDescription = string.Empty;

    public WorldLinksViewModel(
        IWorldApiService worldApi,
        ITreeStateService treeState,
        IUserNotifier notifier,
        ILogger<WorldLinksViewModel> logger)
    {
        _worldApi = worldApi;
        _treeState = treeState;
        _notifier = notifier;
        _logger = logger;
    }

    public List<WorldLinkDto> Links
    {
        get => _links;
        private set => SetField(ref _links, value);
    }

    public bool IsAddingLink
    {
        get => _isAddingLink;
        private set => SetField(ref _isAddingLink, value);
    }

    public bool IsSavingLink
    {
        get => _isSavingLink;
        private set => SetField(ref _isSavingLink, value);
    }

    public string NewLinkTitle
    {
        get => _newLinkTitle;
        set => SetField(ref _newLinkTitle, value);
    }

    public string NewLinkUrl
    {
        get => _newLinkUrl;
        set => SetField(ref _newLinkUrl, value);
    }

    public string NewLinkDescription
    {
        get => _newLinkDescription;
        set => SetField(ref _newLinkDescription, value);
    }

    public Guid? EditingLinkId
    {
        get => _editingLinkId;
        private set => SetField(ref _editingLinkId, value);
    }

    public string EditLinkTitle
    {
        get => _editLinkTitle;
        set => SetField(ref _editLinkTitle, value);
    }

    public string EditLinkUrl
    {
        get => _editLinkUrl;
        set => SetField(ref _editLinkUrl, value);
    }

    public string EditLinkDescription
    {
        get => _editLinkDescription;
        set => SetField(ref _editLinkDescription, value);
    }

    /// <summary>Loads links for the specified world.</summary>
    public async Task LoadAsync(Guid worldId)
    {
        _worldId = worldId;
        Links = await _worldApi.GetWorldLinksAsync(worldId);
    }

    public void StartAddLink()
    {
        IsAddingLink = true;
        NewLinkTitle = string.Empty;
        NewLinkUrl = string.Empty;
        NewLinkDescription = string.Empty;
    }

    public void CancelAddLink()
    {
        IsAddingLink = false;
        NewLinkTitle = string.Empty;
        NewLinkUrl = string.Empty;
        NewLinkDescription = string.Empty;
    }

    public async Task SaveNewLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(NewLinkTitle) || string.IsNullOrWhiteSpace(NewLinkUrl))
        {
            _notifier.Warning("Title and URL are required");
            return;
        }

        if (!IsValidUrl(NewLinkUrl))
        {
            _notifier.Warning("Please enter a valid URL (starting with http:// or https://)");
            return;
        }

        IsSavingLink = true;

        try
        {
            var dto = new WorldLinkCreateDto
            {
                Title = NewLinkTitle.Trim(),
                Url = NewLinkUrl.Trim(),
                Description = string.IsNullOrWhiteSpace(NewLinkDescription) ? null : NewLinkDescription.Trim()
            };

            var created = await _worldApi.CreateWorldLinkAsync(_worldId, dto);
            if (created != null)
            {
                Links = await _worldApi.GetWorldLinksAsync(_worldId);
                await _treeState.RefreshAsync();
                IsAddingLink = false;
                NewLinkTitle = string.Empty;
                NewLinkUrl = string.Empty;
                NewLinkDescription = string.Empty;
                _notifier.Success("Link added");
            }
            else
            {
                _notifier.Error("Failed to add link");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error adding link to world {WorldId}", _worldId);
            _notifier.Error($"Failed to add link: {ex.Message}");
        }
        finally
        {
            IsSavingLink = false;
        }
    }

    public void StartEditLink(WorldLinkDto link)
    {
        EditingLinkId = link.Id;
        EditLinkTitle = link.Title;
        EditLinkUrl = link.Url;
        EditLinkDescription = link.Description ?? string.Empty;
    }

    public void CancelEditLink()
    {
        EditingLinkId = null;
        EditLinkTitle = string.Empty;
        EditLinkUrl = string.Empty;
        EditLinkDescription = string.Empty;
    }

    public async Task SaveEditLinkAsync()
    {
        if (EditingLinkId == null)
            return;

        if (string.IsNullOrWhiteSpace(EditLinkTitle) || string.IsNullOrWhiteSpace(EditLinkUrl))
        {
            _notifier.Warning("Title and URL are required");
            return;
        }

        if (!IsValidUrl(EditLinkUrl))
        {
            _notifier.Warning("Please enter a valid URL (starting with http:// or https://)");
            return;
        }

        IsSavingLink = true;

        try
        {
            var dto = new WorldLinkUpdateDto
            {
                Title = EditLinkTitle.Trim(),
                Url = EditLinkUrl.Trim(),
                Description = string.IsNullOrWhiteSpace(EditLinkDescription) ? null : EditLinkDescription.Trim()
            };

            var updated = await _worldApi.UpdateWorldLinkAsync(_worldId, EditingLinkId.Value, dto);
            if (updated != null)
            {
                Links = await _worldApi.GetWorldLinksAsync(_worldId);
                await _treeState.RefreshAsync();
                EditingLinkId = null;
                EditLinkTitle = string.Empty;
                EditLinkUrl = string.Empty;
                EditLinkDescription = string.Empty;
                _notifier.Success("Link updated");
            }
            else
            {
                _notifier.Error("Failed to update link");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error updating link {LinkId}", EditingLinkId.Value);
            _notifier.Error($"Failed to update link: {ex.Message}");
        }
        finally
        {
            IsSavingLink = false;
        }
    }

    public async Task DeleteLinkAsync(WorldLinkDto link)
    {
        try
        {
            var deleted = await _worldApi.DeleteWorldLinkAsync(_worldId, link.Id);
            if (deleted)
            {
                var updated = new List<WorldLinkDto>(_links);
                updated.Remove(link);
                Links = updated;
                await _treeState.RefreshAsync();
                _notifier.Success("Link deleted");
            }
            else
            {
                _notifier.Error("Failed to delete link");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error deleting link {LinkId}", link.Id);
            _notifier.Error($"Failed to delete link: {ex.Message}");
        }
    }

    /// <summary>Returns a Google favicon URL for the given link URL.</summary>
    public static string GetFaviconUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=32";
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static bool IsValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https");
}
