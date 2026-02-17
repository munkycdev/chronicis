using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.Components.Quests;

public partial class ArcQuestEditor : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public QuestDto? Quest { get; set; }

    [Parameter]
    public EventCallback<QuestDto> OnQuestUpdated { get; set; }

    [Inject] private IQuestApiService QuestApi { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private QuestDto? _quest;
    private string _editTitle = string.Empty;
    private string _editDescription = string.Empty;
    private QuestStatus _editStatus;
    private bool _editIsGmOnly;
    private int _editSortOrder;

    private bool _isSaving;
    private bool _hasUnsavedChanges;
    private bool _editorInitialized;
    private bool _disposed;
    private DotNetObjectReference<ArcQuestEditor>? _dotNetHelper;
    private Timer? _autoSaveTimer;

    private string EditorId => $"quest-desc-editor-{_quest?.Id ?? Guid.Empty}";

    protected override async Task OnParametersSetAsync()
    {
        // Quest changed - dispose old editor and initialize new one
        if (_quest?.Id != Quest?.Id)
        {
            await DisposeEditorAsync();

            _quest = Quest;

            if (_quest != null)
            {
                _editTitle = _quest.Title;
                _editDescription = _quest.Description ?? string.Empty;
                _editStatus = _quest.Status;
                _editIsGmOnly = _quest.IsGmOnly;
                _editSortOrder = _quest.SortOrder;
                _hasUnsavedChanges = false;
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
        }

        if (_quest != null && !_editorInitialized && !_disposed && _dotNetHelper != null)
        {
            await Task.Delay(100); // Small delay to ensure DOM is ready
            await InitializeEditorAsync();
        }
    }

    private async Task InitializeEditorAsync()
    {
        if (_editorInitialized || _disposed || _dotNetHelper == null)
            return;

        try
        {
            await JSRuntime.InvokeVoidAsync("initializeTipTapEditor", EditorId, _editDescription, _dotNetHelper);

            if (_disposed)
                return;

            await JSRuntime.InvokeVoidAsync("initializeWikiLinkAutocomplete", EditorId, _dotNetHelper);
            _editorInitialized = true;
        }
        catch (ObjectDisposedException)
        {
            // Expected during navigation
        }
        catch (JSDisconnectedException)
        {
            // Expected during navigation
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                Snackbar.Add($"Failed to initialize quest editor: {ex.Message}", Severity.Warning);
            }
        }
    }

    private async Task DisposeEditorAsync()
    {
        if (_editorInitialized && !_disposed)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("destroyTipTapEditor", EditorId);
            }
            catch
            {
                // Ignore errors during disposal
            }
            finally
            {
                _editorInitialized = false;
            }
        }
    }

    [JSInvokable]
    public void OnEditorUpdate(string html)
    {
        _editDescription = html;
        _hasUnsavedChanges = true;

        // Debounce auto-save (0.5s delay)
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(async _ => await AutoSaveAsync(), null, 500, Timeout.Infinite);
    }

    private async Task AutoSaveAsync()
    {
        await InvokeAsync(async () =>
        {
            if (_hasUnsavedChanges && !_isSaving)
            {
                await SaveQuestAsync();
            }
        });
    }

    private async Task OnTitleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SaveTitleAsync();
        }
    }

    private async Task SaveTitleAsync()
    {
        if (_quest != null && _editTitle != _quest.Title)
        {
            _hasUnsavedChanges = true;
            await SaveQuestAsync();
        }
    }

    private void OnStatusChanged(QuestStatus newStatus)
    {
        _editStatus = newStatus;
        _ = SaveQuestAsync();
    }

    private void OnGmOnlyChanged(bool newValue)
    {
        _editIsGmOnly = newValue;
        _ = SaveQuestAsync();
    }

    private void OnSortOrderChanged(int newValue)
    {
        _editSortOrder = newValue;
        _ = SaveQuestAsync();
    }

    private async Task SaveQuestAsync()
    {
        if (_quest == null || _isSaving)
            return;

        _isSaving = true;
        StateHasChanged();

        try
        {
            var editDto = new QuestEditDto
            {
                Title = string.IsNullOrWhiteSpace(_editTitle) ? null : _editTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(_editDescription) ? null : _editDescription,
                Status = _editStatus,
                IsGmOnly = _editIsGmOnly,
                SortOrder = _editSortOrder,
                RowVersion = _quest.RowVersion
            };

            var updated = await QuestApi.UpdateQuestAsync(_quest.Id, editDto);

            if (updated != null)
            {
                // Success - update local state with server response
                _quest = updated;
                _editTitle = updated.Title;
                _editDescription = updated.Description ?? string.Empty;
                _editStatus = updated.Status;
                _editIsGmOnly = updated.IsGmOnly;
                _editSortOrder = updated.SortOrder;
                _hasUnsavedChanges = false;

                await OnQuestUpdated.InvokeAsync(updated);
            }
            else
            {
                // 409 conflict was already handled by QuestApiService
                // Reload from server
                var current = await QuestApi.GetQuestAsync(_quest.Id);
                if (current != null)
                {
                    _quest = current;
                    _editTitle = current.Title;
                    _editDescription = current.Description ?? string.Empty;
                    _editStatus = current.Status;
                    _editIsGmOnly = current.IsGmOnly;
                    _editSortOrder = current.SortOrder;

                    // Reinitialize editor with server content
                    await DisposeEditorAsync();
                    await InitializeEditorAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save quest: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _autoSaveTimer?.Dispose();

        // Properly dispose TipTap editor
        await DisposeEditorAsync();

        _dotNetHelper?.Dispose();

        GC.SuppressFinalize(this);
    }
}
