using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Utilities;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the ArticleDetail component.
/// Owns pure C# business logic: loading, saving, deleting, and creating articles.
/// JS-interop concerns (TipTap editor lifecycle, autocomplete cursor position,
/// external link preview, DotNetObjectReference) remain in the component.
/// </summary>
public sealed class ArticleDetailViewModel : ViewModelBase
{
    private readonly IArticleApiService _articleApi;
    private readonly ILinkApiService _linkApi;
    private readonly ITreeStateService _treeState;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly IAppContextService _appContext;
    private readonly IArticleCacheService _articleCache;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly IPageTitleService _titleService;
    private readonly ILogger<ArticleDetailViewModel> _logger;

    private ArticleDto? _article;
    private List<BreadcrumbItem>? _breadcrumbs;
    private string _editTitle = string.Empty;
    private string _editBody = string.Empty;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isAutoLinking;
    private bool _isCreatingChild;
    private bool _hasUnsavedChanges;
    private bool _isSummaryExpanded;
    private string _lastSaveTime = "just now";

    public ArticleDetailViewModel(
        IArticleApiService articleApi,
        ILinkApiService linkApi,
        ITreeStateService treeState,
        IBreadcrumbService breadcrumbService,
        IAppContextService appContext,
        IArticleCacheService articleCache,
        IAppNavigator navigator,
        IUserNotifier notifier,
        IPageTitleService titleService,
        ILogger<ArticleDetailViewModel> logger)
    {
        _articleApi = articleApi;
        _linkApi = linkApi;
        _treeState = treeState;
        _breadcrumbService = breadcrumbService;
        _appContext = appContext;
        _articleCache = articleCache;
        _navigator = navigator;
        _notifier = notifier;
        _titleService = titleService;
        _logger = logger;
    }

    // ---------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------

    public ArticleDto? Article
    {
        get => _article;
        private set => SetField(ref _article, value);
    }

    public List<BreadcrumbItem>? Breadcrumbs
    {
        get => _breadcrumbs;
        private set => SetField(ref _breadcrumbs, value);
    }

    public string EditTitle
    {
        get => _editTitle;
        set
        {
            if (SetField(ref _editTitle, value))
                HasUnsavedChanges = true;
        }
    }

    public string EditBody
    {
        get => _editBody;
        set
        {
            if (SetField(ref _editBody, value))
                HasUnsavedChanges = true;
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetField(ref _isSaving, value);
    }

    public bool IsAutoLinking
    {
        get => _isAutoLinking;
        private set => SetField(ref _isAutoLinking, value);
    }

    public bool IsCreatingChild
    {
        get => _isCreatingChild;
        private set => SetField(ref _isCreatingChild, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetField(ref _hasUnsavedChanges, value);
    }

    public bool IsSummaryExpanded
    {
        get => _isSummaryExpanded;
        set => SetField(ref _isSummaryExpanded, value);
    }

    public string LastSaveTime
    {
        get => _lastSaveTime;
        private set => SetField(ref _lastSaveTime, value);
    }

    // ---------------------------------------------------------------------------
    // Loading
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Hydrates VM state from an already-fetched article.
    /// </summary>
    public async Task HydrateArticleAsync(ArticleDto article)
    {
        Article = article;
        EditTitle = article.Title ?? string.Empty;
        EditBody = article.Body ?? string.Empty;
        HasUnsavedChanges = false;

        _articleCache.CacheArticle(article);
        RefreshBreadcrumbs();

        await _titleService.SetTitleAsync(
            string.IsNullOrEmpty(article.Title) ? "Untitled" : article.Title);
    }

    /// <summary>
    /// Loads an article by ID and populates all VM state.
    /// Returns true if the article was loaded successfully.
    /// </summary>
    public async Task<bool> LoadArticleAsync(Guid articleId)
    {
        IsLoading = true;

        try
        {
            var article = await _articleApi.GetArticleAsync(articleId);
            Article = article;
            EditTitle = article?.Title ?? string.Empty;
            EditBody = article?.Body ?? string.Empty;
            HasUnsavedChanges = false;

            if (article != null)
                _articleCache.CacheArticle(article);

            RefreshBreadcrumbs();
            await _titleService.SetTitleAsync(
                string.IsNullOrEmpty(article?.Title) ? "Untitled" : article.Title);

            return article != null;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to load article {ArticleId}", articleId);
            _notifier.Error($"Failed to load article: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Clears article state (called when tree selection is cleared).</summary>
    public void ClearArticle()
    {
        Article = null;
        Breadcrumbs = null;
        EditTitle = string.Empty;
        EditBody = string.Empty;
        HasUnsavedChanges = false;
        _ = _titleService.SetTitleAsync("Chronicis");
    }

    // ---------------------------------------------------------------------------
    // Save
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Saves the current article. The caller must pass the current editor values
    /// (retrieved from the razor-bound fields / JS TipTap editor) as <paramref name="currentBody"/>
    /// and <paramref name="currentTitle"/>. Both are required: there is no bidirectional
    /// binding between razor state and VM state, so the VM cannot observe edits directly.
    /// </summary>
    public async Task<SaveArticleResult> SaveArticleAsync(string currentBody, string currentTitle)
    {
        if (_article == null || IsSaving)
            return SaveArticleResult.Skipped;

        // Sync editor state into VM before saving.
        _editBody = currentBody;
        EditTitle = currentTitle ?? string.Empty;
        IsSaving = true;

        try
        {
            var originalTitle = _article.Title;
            var newTitle = EditTitle.Trim();
            var titleChanged = originalTitle != newTitle;
            string? newSlug = null;

            if (titleChanged)
            {
                var suggestedSlug = SlugGenerator.GenerateSlug(newTitle);
                if (suggestedSlug != _article.Slug)
                    newSlug = suggestedSlug;
            }

            var updateDto = new ArticleUpdateDto
            {
                Title = newTitle,
                Slug = newSlug,
                Body = _editBody,
                EffectiveDate = _article.EffectiveDate,
                IconEmoji = _article.IconEmoji
            };

            var updated = await _articleApi.UpdateArticleAsync(_article.Id, updateDto);
            if (updated == null)
            {
                // API returned null (non-2xx or deserialization failure).
                // Do NOT show success, do NOT mutate local state — the server did not accept the change.
                _notifier.Error("Failed to save article. Please try again.");
                return SaveArticleResult.Failed;
            }

            _articleCache.InvalidateCache();

            // Use the server's response as the source of truth rather than optimistic local mutation.
            _article.Title = updated.Title;
            _article.Slug = updated.Slug;
            _article.Body = updated.Body;
            _article.ModifiedAt = updated.ModifiedAt;
            _article.IconEmoji = updated.IconEmoji;
            HasUnsavedChanges = false;
            LastSaveTime = "just now";

            bool slugChanged = newSlug != null;

            if (titleChanged || slugChanged)
            {
                await _titleService.SetTitleAsync(newTitle);
                _treeState.UpdateNodeDisplay(_article.Id, newTitle, _article.IconEmoji);
            }

            if (slugChanged)
            {
                _treeState.ExpandPathToAndSelect(_article.Id);
                var refreshed = await _articleApi.GetArticleAsync(_article.Id);
                Article = refreshed;

                if (refreshed != null)
                    await _navigator.GoToArticleAsync(refreshed);
            }

            return SaveArticleResult.Saved;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to save article {ArticleId}", _article?.Id);
            _notifier.Error($"Failed to save: {ex.Message}");
            return SaveArticleResult.Failed;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ---------------------------------------------------------------------------
    // Delete
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Deletes the current article. Returns true on success.
    /// Caller is responsible for the confirmation dialog (JS or MudBlazor).
    /// </summary>
    public async Task<bool> DeleteArticleAsync()
    {
        if (_article == null)
            return false;

        try
        {
            var success = await _articleApi.DeleteArticleAsync(_article.Id);
            if (success)
            {
                _articleCache.InvalidateCache();
                _notifier.Success("Article deleted successfully");
                await _treeState.RefreshAsync();
                Article = null;
                return true;
            }
            else
            {
                _notifier.Error("Failed to delete article. You may not have permission.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to delete article {ArticleId}", _article?.Id);
            _notifier.Error($"Failed to delete: {ex.Message}");
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    // Icon
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Saves an icon change immediately without a full save cycle.
    /// </summary>
    public async Task HandleIconChangedAsync(string? newIcon)
    {
        if (_article == null || IsSaving)
            return;

        _article.IconEmoji = newIcon;

        try
        {
            var updateDto = new ArticleUpdateDto
            {
                Title = _article.Title,
                Slug = _article.Slug,
                Body = _article.Body,
                EffectiveDate = _article.EffectiveDate,
                IconEmoji = newIcon
            };

            await _articleApi.UpdateArticleAsync(_article.Id, updateDto);
            _treeState.UpdateNodeDisplay(_article.Id, _article.Title, newIcon);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to save icon change for article {ArticleId}", _article?.Id);
            _notifier.Error($"Failed to save icon: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------------
    // Article Creation
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a root-level article in the current world.
    /// Returns the created article's slug for navigation, or null on failure.
    /// </summary>
    public async Task<string?> CreateRootArticleAsync()
    {
        var worldId = _appContext.CurrentWorldId;
        if (!worldId.HasValue || worldId == Guid.Empty)
        {
            _notifier.Warning("Please select a World first");
            return null;
        }

        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = "Untitled",
                Body = string.Empty,
                ParentId = null,
                EffectiveDate = DateTime.Now,
                WorldId = worldId.Value
            };

            await _articleApi.CreateArticleAsync(createDto);
            await _treeState.RefreshAsync();
            _notifier.Success("Article created");
            return null; // root creation refreshes tree; no navigation needed
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to create root article");
            _notifier.Error($"Failed to create article: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a sibling article (same parent as current) and navigates to it.
    /// Returns true on success, false on failure.
    /// </summary>
    public async Task<bool> CreateSiblingArticleAsync()
    {
        if (_article == null)
            return false;

        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = string.Empty,
                Body = string.Empty,
                ParentId = _article.ParentId,
                EffectiveDate = DateTime.Now,
                WorldId = _article.WorldId,
                CampaignId = _article.CampaignId
            };

            var created = await _articleApi.CreateArticleAsync(createDto);
            if (created == null)
            {
                _notifier.Error("Failed to create article");
                return false;
            }

            await _treeState.RefreshAsync();
            _treeState.ExpandPathToAndSelect(created.Id);

            var detail = await _articleApi.GetArticleDetailAsync(created.Id);
            _notifier.Success("New article created");

            if (detail != null)
                await _navigator.GoToArticleAsync(detail);

            return detail != null;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to create sibling article");
            _notifier.Error($"Failed to create article: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a child article under the current article and navigates to it.
    /// Returns true on success, false on failure.
    /// </summary>
    public async Task<bool> CreateChildArticleAsync()
    {
        if (_article == null || IsCreatingChild)
            return false;

        IsCreatingChild = true;

        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = string.Empty,
                Body = string.Empty,
                ParentId = _article.Id,
                EffectiveDate = DateTime.Now,
                WorldId = _article.WorldId,
                CampaignId = _article.CampaignId
            };

            var created = await _articleApi.CreateArticleAsync(createDto);
            if (created == null)
            {
                _notifier.Error("Failed to create article");
                return false;
            }

            await _treeState.RefreshAsync();
            _treeState.ExpandPathToAndSelect(created.Id);

            var detail = await _articleApi.GetArticleDetailAsync(created.Id);
            _notifier.Success("New article created");

            if (detail != null)
                await _navigator.GoToArticleAsync(detail);

            return detail != null;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to create child article");
            _notifier.Error($"Failed to create article: {ex.Message}");
            return false;
        }
        finally
        {
            IsCreatingChild = false;
        }
    }

    // ---------------------------------------------------------------------------
    // Auto-link
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Fetches auto-link suggestions for the current article and current body content.
    /// Returns the result for the caller to display a confirmation dialog,
    /// or null if nothing was found or an error occurred.
    /// </summary>
    public async Task<AutoLinkResponseDto?> FetchAutoLinkSuggestionsAsync(string currentBody)
    {
        if (_article == null || IsAutoLinking)
            return null;

        IsAutoLinking = true;

        try
        {
            var result = await _linkApi.AutoLinkAsync(_article.Id, currentBody);

            if (result == null)
            {
                _notifier.Error("Failed to scan for links");
                return null;
            }

            if (result.LinksFound == 0)
            {
                _notifier.Info("No linkable content found");
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error auto-linking article {ArticleId}", _article?.Id);
            _notifier.Error($"Failed to auto-link: {ex.Message}");
            return null;
        }
        finally
        {
            IsAutoLinking = false;
        }
    }

    /// <summary>
    /// Called after the component has applied wiki links via JS.
    /// Syncs the updated body and triggers a save.
    /// </summary>
    public async Task CommitAutoLinkAsync(string updatedBody, int linksApplied)
    {
        _editBody = updatedBody;
        HasUnsavedChanges = true;
        // Auto-link only modifies body; forward the VM's current title so the
        // save path still satisfies the required title parameter.
        await SaveArticleAsync(updatedBody, EditTitle);
        _notifier.Success($"Added {linksApplied} link(s)");
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private void RefreshBreadcrumbs()
    {
        if (_article?.Breadcrumbs != null && _article.Breadcrumbs.Any())
            Breadcrumbs = _breadcrumbService.ForArticle(_article);
        else
            Breadcrumbs = new List<BreadcrumbItem> { new("Dashboard", href: "/dashboard") };
    }

    /// <summary>Generates the delete confirmation message including child count warning.</summary>
    public string GetDeleteConfirmationMessage()
    {
        if (_article == null)
            return string.Empty;

        var message = $"Are you sure you want to delete '{_article.Title}'?";
        if (_article.ChildCount > 0)
        {
            var childText = _article.ChildCount == 1
                ? "1 child article"
                : $"{_article.ChildCount} child articles";
            message = $"Are you sure you want to delete '{_article.Title}'?\n\n⚠️ WARNING: This will also delete {childText} and all their descendants.";
        }

        return message + "\n\nThis action cannot be undone.";
    }
}

/// <summary>
/// Discriminated union result for <see cref="ArticleDetailViewModel.SaveArticleAsync"/>.
/// </summary>
public sealed class SaveArticleResult
{
    public enum ResultKind { Skipped, Saved, Failed }

    public ResultKind Kind { get; }

    private SaveArticleResult(ResultKind kind)
    {
        Kind = kind;
    }

    public static readonly SaveArticleResult Skipped = new(ResultKind.Skipped);
    public static readonly SaveArticleResult Saved = new(ResultKind.Saved);
    public static readonly SaveArticleResult Failed = new(ResultKind.Failed);
}
