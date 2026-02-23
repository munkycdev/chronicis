using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.World;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel managing document CRUD for a world's Resources tab.
/// </summary>
public sealed class WorldDocumentsViewModel : ViewModelBase
{
    private readonly IWorldApiService _worldApi;
    private readonly ITreeStateService _treeState;
    private readonly IUserNotifier _notifier;
    private readonly IDialogService _dialogService;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<WorldDocumentsViewModel> _logger;

    private Guid _worldId;
    private List<WorldDocumentDto> _documents = new();
    private Guid? _editingDocumentId;
    private string _editDocumentTitle = string.Empty;
    private string _editDocumentDescription = string.Empty;
    private bool _isSavingDocument;

    public WorldDocumentsViewModel(
        IWorldApiService worldApi,
        ITreeStateService treeState,
        IUserNotifier notifier,
        IDialogService dialogService,
        IAppNavigator navigator,
        ILogger<WorldDocumentsViewModel> logger)
    {
        _worldApi = worldApi;
        _treeState = treeState;
        _notifier = notifier;
        _dialogService = dialogService;
        _navigator = navigator;
        _logger = logger;
    }

    public List<WorldDocumentDto> Documents
    {
        get => _documents;
        private set => SetField(ref _documents, value);
    }

    public Guid? EditingDocumentId
    {
        get => _editingDocumentId;
        private set => SetField(ref _editingDocumentId, value);
    }

    public string EditDocumentTitle
    {
        get => _editDocumentTitle;
        set => SetField(ref _editDocumentTitle, value);
    }

    public string EditDocumentDescription
    {
        get => _editDocumentDescription;
        set => SetField(ref _editDocumentDescription, value);
    }

    public bool IsSavingDocument
    {
        get => _isSavingDocument;
        private set => SetField(ref _isSavingDocument, value);
    }

    /// <summary>Loads documents for the specified world.</summary>
    public async Task LoadAsync(Guid worldId)
    {
        _worldId = worldId;
        Documents = await _worldApi.GetWorldDocumentsAsync(worldId);
    }

    public async Task OpenUploadDialogAsync(Guid worldId)
    {
        var parameters = new DialogParameters { { "WorldId", worldId } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        var dialog = await _dialogService.ShowAsync<WorldDocumentUploadDialog>("Upload Document", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Documents = await _worldApi.GetWorldDocumentsAsync(worldId);
            await _treeState.RefreshAsync();
        }
    }

    public void StartEditDocument(WorldDocumentDto document)
    {
        EditingDocumentId = document.Id;
        EditDocumentTitle = document.Title;
        EditDocumentDescription = document.Description ?? string.Empty;
    }

    public void CancelDocumentEdit()
    {
        EditingDocumentId = null;
        EditDocumentTitle = string.Empty;
        EditDocumentDescription = string.Empty;
    }

    public async Task SaveDocumentEditAsync()
    {
        if (EditingDocumentId == null)
            return;

        IsSavingDocument = true;

        try
        {
            var updateDto = new WorldDocumentUpdateDto
            {
                Title = EditDocumentTitle,
                Description = EditDocumentDescription
            };

            var updated = await _worldApi.UpdateDocumentAsync(_worldId, EditingDocumentId.Value, updateDto);

            if (updated != null)
            {
                var doc = _documents.FirstOrDefault(d => d.Id == EditingDocumentId.Value);
                if (doc != null)
                {
                    doc.Title = updated.Title;
                    doc.Description = updated.Description;
                }

                EditingDocumentId = null;
                EditDocumentTitle = string.Empty;
                EditDocumentDescription = string.Empty;

                await _treeState.RefreshAsync();
                _notifier.Success("Document updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error updating document {DocumentId}", EditingDocumentId!.Value);
            _notifier.Error($"Failed to update document: {ex.Message}");
        }
        finally
        {
            IsSavingDocument = false;
        }
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId);
        if (doc == null)
            return;

        try
        {
            var success = await _worldApi.DeleteDocumentAsync(_worldId, documentId);

            if (success)
            {
                var updated = new List<WorldDocumentDto>(_documents);
                updated.Remove(doc);
                Documents = updated;
                await _treeState.RefreshAsync();
                _notifier.Success("Document deleted");
            }
            else
            {
                _notifier.Error("Failed to delete document");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error deleting document {DocumentId}", documentId);
            _notifier.Error($"Error deleting document: {ex.Message}");
        }
    }

    public async Task DownloadDocumentAsync(Guid documentId)
    {
        try
        {
            var download = await _worldApi.DownloadDocumentAsync(documentId);

            if (download == null)
            {
                _notifier.Error("Failed to get download URL");
                return;
            }

            _navigator.NavigateTo(download.DownloadUrl);
            _notifier.Success($"Opening {download.FileName}");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error downloading document {DocumentId}", documentId);
            _notifier.Error($"Error downloading document: {ex.Message}");
        }
    }

    /// <summary>Returns a MudBlazor icon string for the given MIME content type.</summary>
    public static string GetDocumentIcon(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "application/pdf" => Icons.Material.Filled.PictureAsPdf,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => Icons.Material.Filled.Description,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => Icons.Material.Filled.TableChart,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => Icons.Material.Filled.Slideshow,
            "text/plain" => Icons.Material.Filled.TextSnippet,
            "text/markdown" => Icons.Material.Filled.Article,
            string ct when ct.StartsWith("image/") => Icons.Material.Filled.Image,
            _ => Icons.Material.Filled.InsertDriveFile
        };

    /// <summary>Formats a byte count as a human-readable file size string.</summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
