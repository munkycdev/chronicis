using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.Components.Quests;

public partial class QuestDrawer : IAsyncDisposable
{
    [Inject] private IArticleApiService ArticleApi { get; set; } = null!;

    [Parameter]
    public bool IsOpen { get; set; }

    private bool _isLoading;
    private bool _isSubmitting;
    private bool _loadingUpdates;
    private string? _emptyStateMessage;
    private string? _loadingError;
    private string? _validationError;

    private List<QuestDto>? _quests;
    private Guid? _selectedQuestId;
    private QuestDto? _selectedQuest;
    private List<QuestUpdateEntryDto>? _recentUpdates;

    private Guid? _currentArcId;
    private Guid? _currentSessionId;
    private Guid? _currentWorldId;
    private bool _canAssociateSession;
    private bool _associateWithSession = true;

    private IJSObjectReference? _editorModule;
    private bool _editorInitialized;
    private DotNetObjectReference<QuestDrawer>? _dotNetRef;
    private bool _disposed;
    private bool _questsLoadedForArc;
    private bool _needsEditorInit; // Flag to trigger editor init after render
    private bool _lastIsOpen;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen == _lastIsOpen)
        {
            return;
        }

        _lastIsOpen = IsOpen;

        if (IsOpen)
        {
            await HandleOpenAsync();
        }
        else
        {
            await HandleCloseAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Initialize editor after DOM has been updated
        if (_needsEditorInit && !_editorInitialized)
        {
            _needsEditorInit = false;
            await InitializeEditorAsync();
        }
    }

    private async Task HandleOpenAsync()
    {
        await LoadQuestsAsync();
        StateHasChanged();

        // Focus the first quest selector after render
        if (_quests?.Any() == true)
        {
            await Task.Delay(100);
            await FocusFirstQuestAsync();
        }
    }

    private async Task HandleCloseAsync()
    {
        await DisposeEditorAsync();
        _selectedQuestId = null;
        _selectedQuest = null;
        _recentUpdates = null;
        _validationError = null;
        _questsLoadedForArc = false;
        _quests = null;
        StateHasChanged();
    }

    private async Task CloseDrawer()
    {
        QuestDrawerService.Close();

        // Give the drawer host time to animate closed, then return focus to main content.
        await Task.Delay(200);
        await RestoreFocusAsync();
    }

    private async Task LoadQuestsAsync()
    {
        _isLoading = true;
        _emptyStateMessage = null;
        _loadingError = null;

        try
        {
            var selectedNodeId = TreeState.SelectedNodeId;
            if (!selectedNodeId.HasValue)
            {
                _emptyStateMessage = "No article selected. Navigate to a session to use quest tracking.";
                return;
            }

            Guid? incomingArcId;

            if (TreeState.TryGetNode(selectedNodeId.Value, out var selectedNode)
                && selectedNode != null
                && selectedNode.NodeType == TreeNodeType.Session)
            {
                _currentWorldId = selectedNode.WorldId;
                incomingArcId = selectedNode.ArcId;
                _currentSessionId = selectedNode.Id;
                _canAssociateSession = true;
            }
            else
            {
                // Resolve Arc and Session from current article
                var selectedArticle = await GetCurrentArticleAsync();

                if (selectedArticle == null)
                {
                    _emptyStateMessage = "No article selected. Navigate to a session to use quest tracking.";
                    return;
                }

                // Store the world ID for autocomplete
                _currentWorldId = selectedArticle.WorldId;

                if (selectedArticle.Type != ArticleType.Session && selectedArticle.Type != ArticleType.SessionNote)
                {
                    _emptyStateMessage = "Navigate to a session or session note to use quest tracking.";
                    return;
                }

                incomingArcId = selectedArticle.ArcId;
                if (selectedArticle.Type == ArticleType.Session)
                {
                    // Legacy session article IDs were migrated to Session entity IDs 1:1 in Phase 1.
                    _currentSessionId = selectedArticle.Id;
                    _canAssociateSession = true;
                }
                else
                {
                    _currentSessionId = await ResolveSessionIdFromParentAsync(selectedArticle.Id);
                    _canAssociateSession = _currentSessionId.HasValue;
                }
            }

            if (!incomingArcId.HasValue)
            {
                _emptyStateMessage = "This session is not associated with an arc.";
                return;
            }

            // Reset cache if the arc has changed
            if (_currentArcId != incomingArcId)
            {
                _questsLoadedForArc = false;
                _quests = null;
                _selectedQuestId = null;
                _selectedQuest = null;
                _recentUpdates = null;
            }

            _currentArcId = incomingArcId;

            // Only load quests if we haven't already loaded for this arc (prevent duplicate fetches)
            if (!_questsLoadedForArc)
            {
                var allQuests = await QuestApi.GetArcQuestsAsync(_currentArcId.Value);
                // The drawer is a player-facing view — GM-only quests are never shown here
                _quests = allQuests.Where(q => !q.IsGmOnly).ToList();
                _questsLoadedForArc = true;
            }

            // Auto-select first quest if available and none selected
            if (_quests?.Any() == true && !_selectedQuestId.HasValue)
            {
                await SelectQuest(_quests.First().Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load quests");
            _loadingError = ex.Message;
            _quests = null;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task<ArticleDto?> GetCurrentArticleAsync()
    {
        var selectedNodeId = TreeState.SelectedNodeId;

        if (!selectedNodeId.HasValue)
            return null;

        try
        {
            return await ArticleApi.GetArticleDetailAsync(selectedNodeId.Value);
        }
        catch
        {
            return null;
        }
    }

    private async Task<Guid?> ResolveSessionIdFromParentAsync(Guid articleId)
    {
        try
        {
            var article = await ArticleApi.GetArticleDetailAsync(articleId);
            return article?.SessionId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resolve session ID");
        }

        return null;
    }

    private async Task SelectQuest(Guid questId)
    {
        if (_selectedQuestId == questId)
            return;

        // Dispose old editor if switching quests
        if (_editorInitialized)
        {
            await DisposeEditorAsync();
        }

        _selectedQuestId = questId;
        _selectedQuest = _quests?.FirstOrDefault(q => q.Id == questId);
        _validationError = null;

        if (_selectedQuest != null)
        {
            // Load recent updates
            await LoadRecentUpdatesAsync(questId);

            // Set flag to initialize editor after next render
            _needsEditorInit = true;
        }

        // Trigger render - OnAfterRenderAsync will handle editor init
        StateHasChanged();
    }

    private async Task HandleQuestItemKeyDown(KeyboardEventArgs e, Guid questId)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            await SelectQuest(questId);
        }
    }

    private async Task LoadRecentUpdatesAsync(Guid questId)
    {
        _loadingUpdates = true;
        StateHasChanged();

        try
        {
            var result = await QuestApi.GetQuestUpdatesAsync(questId, skip: 0, take: 5);
            _recentUpdates = result.Items;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load quest updates");
            _recentUpdates = new List<QuestUpdateEntryDto>();
        }
        finally
        {
            _loadingUpdates = false;
            StateHasChanged();
        }
    }

    private async Task InitializeEditorAsync()
    {
        if (_editorInitialized || _disposed)
            return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _editorModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/questEditor.js");

            await _editorModule.InvokeVoidAsync("initializeEditor", "quest-update-editor", _dotNetRef);
            _editorInitialized = true;

            // Focus the editor after initialization
            await Task.Delay(50);
            await FocusEditorAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize quest editor");
            Snackbar.Add("Failed to initialize editor", Severity.Warning);
        }
    }

    private async Task DisposeEditorAsync()
    {
        if (!_editorInitialized || _disposed)
            return;

        try
        {
            if (_editorModule != null)
            {
                await _editorModule.InvokeVoidAsync("destroyEditor");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing quest editor");
        }
        finally
        {
            _editorInitialized = false;
        }
    }

    private async Task SubmitUpdate()
    {
        if (_selectedQuest == null || _isSubmitting)
            return;

        _isSubmitting = true;
        _validationError = null;

        try
        {
            // Get editor content
            if (_editorModule == null)
            {
                _validationError = "Editor not initialized";
                return;
            }

            var content = await _editorModule.InvokeAsync<string>("getEditorContent");

            // Validate content is not empty or whitespace
            if (string.IsNullOrWhiteSpace(content) || content == "<p></p>" || content == "<p><br></p>")
            {
                _validationError = "Update content cannot be empty";
                return;
            }

            // Create update DTO
            var createDto = new QuestUpdateCreateDto
            {
                Body = content,
                SessionId = _associateWithSession && _canAssociateSession ? _currentSessionId : null
            };

            // Submit via API
            var result = await QuestApi.AddQuestUpdateAsync(_selectedQuest.Id, createDto);

            if (result != null)
            {
                // Clear editor
                await _editorModule.InvokeVoidAsync("clearEditor");

                // Refresh updates list
                await LoadRecentUpdatesAsync(_selectedQuest.Id);

                // Update the quest's UpdatedAt (refresh from server if needed)
                var refreshedQuest = await QuestApi.GetQuestAsync(_selectedQuest.Id);
                if (refreshedQuest != null)
                {
                    var questInList = _quests?.FirstOrDefault(q => q.Id == _selectedQuest.Id);
                    if (questInList != null)
                    {
                        questInList.UpdatedAt = refreshedQuest.UpdatedAt;
                        questInList.UpdateCount = refreshedQuest.UpdateCount;
                    }

                    _selectedQuest = refreshedQuest;
                }

                Snackbar.Add("Quest update added", Severity.Success);
            }
            else
            {
                _validationError = "Failed to add quest update";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to submit quest update");
            _validationError = $"Error: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task FocusFirstQuestAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval",
                "document.querySelector('.quest-item')?.focus()");
        }
        catch
        {
            // Ignore focus errors
        }
    }

    private async Task FocusEditorAsync()
    {
        try
        {
            if (_editorModule != null)
            {
                await _editorModule.InvokeVoidAsync("focusEditor");
            }
        }
        catch
        {
            // Ignore focus errors
        }
    }

    private async Task RestoreFocusAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval",
                "document.querySelector('.chronicis-article-card')?.focus() || document.querySelector('main')?.focus()");
        }
        catch
        {
            // Ignore focus errors
        }
    }

    #region Wiki Link Autocomplete

    [JSInvokable]
    public async Task OnAutocompleteTriggered(string query, double x, double y)
    {
        await AutocompleteService.ShowAsync(query, x, y, _currentWorldId);
    }

    [JSInvokable]
    public Task OnAutocompleteHidden()
    {
        AutocompleteService.Hide();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnAutocompleteArrowDown()
    {
        AutocompleteService.SelectNext();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnAutocompleteArrowUp()
    {
        AutocompleteService.SelectPrevious();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnAutocompleteEnter()
    {
        var selected = AutocompleteService.GetSelectedSuggestion();
        if (selected != null)
        {
            await HandleAutocompleteSuggestionSelected(selected);
        }
    }

    private async Task HandleAutocompleteSuggestionSelected(WikiLinkAutocompleteItem suggestion)
    {
        if (_editorModule == null)
            return;

        try
        {
            if (suggestion.IsExternal)
            {
                // Insert external link: [[srd/spell-name]]
                var linkText = $"{AutocompleteService.ExternalSourceKey}/{suggestion.ExternalKey}";
                await _editorModule.InvokeVoidAsync("insertWikiLink", linkText, suggestion.DisplayText);
            }
            else
            {
                // Insert internal article link: [[article-title]]
                await _editorModule.InvokeVoidAsync("insertWikiLink", suggestion.DisplayText, null);
            }

            // Hide autocomplete
            AutocompleteService.Hide();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to insert wiki link suggestion");
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Properly dispose TipTap editor
        await DisposeEditorAsync();

        _dotNetRef?.Dispose();

        if (_editorModule != null)
        {
            await _editorModule.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
