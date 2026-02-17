using Blazored.LocalStorage;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.Services;

/// <summary>
/// Facade that wraps 16 services used by ArticleDetail component.
/// Implemented using TDD (Test-Driven Development) methodology.
/// 
/// Benefits:
/// - Simplifies component testing by reducing mock dependencies from 16 to 1
/// - Provides clear API for ArticleDetail functionality
/// - Centralizes service coordination logic
/// - Makes it easier to refactor service dependencies without changing components
/// </summary>
public class ArticleDetailFacade : IArticleDetailFacade
{
    private readonly IArticleApiService _articleApi;
    private readonly ILinkApiService _linkApi;
    private readonly IExternalLinkApiService _externalLinkApi;
    private readonly IWikiLinkService _wikiLinkService;
    private readonly IMarkdownService _markdownService;
    private readonly NavigationManager _navigationManager;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly ITreeStateService _treeState;
    private readonly IAppContextService _appContext;
    private readonly ISnackbar _snackbar;
    private readonly IMetadataDrawerService _metadataDrawerService;
    private readonly IKeyboardShortcutService _keyboardShortcutService;
    private readonly IArticleCacheService _articleCache;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ArticleDetailFacade> _logger;

    public ArticleDetailFacade(
        IArticleApiService articleApi,
        ILinkApiService linkApi,
        IExternalLinkApiService externalLinkApi,
        IWikiLinkService wikiLinkService,
        IMarkdownService markdownService,
        NavigationManager navigationManager,
        IBreadcrumbService breadcrumbService,
        ITreeStateService treeState,
        IAppContextService appContext,
        ISnackbar snackbar,
        IMetadataDrawerService metadataDrawerService,
        IKeyboardShortcutService keyboardShortcutService,
        IArticleCacheService articleCache,
        ILocalStorageService localStorage,
        IJSRuntime jsRuntime,
        ILogger<ArticleDetailFacade> logger)
    {
        _articleApi = articleApi ?? throw new ArgumentNullException(nameof(articleApi));
        _linkApi = linkApi ?? throw new ArgumentNullException(nameof(linkApi));
        _externalLinkApi = externalLinkApi ?? throw new ArgumentNullException(nameof(externalLinkApi));
        _wikiLinkService = wikiLinkService ?? throw new ArgumentNullException(nameof(wikiLinkService));
        _markdownService = markdownService ?? throw new ArgumentNullException(nameof(markdownService));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _breadcrumbService = breadcrumbService ?? throw new ArgumentNullException(nameof(breadcrumbService));
        _treeState = treeState ?? throw new ArgumentNullException(nameof(treeState));
        _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
        _metadataDrawerService = metadataDrawerService ?? throw new ArgumentNullException(nameof(metadataDrawerService));
        _keyboardShortcutService = keyboardShortcutService ?? throw new ArgumentNullException(nameof(keyboardShortcutService));
        _articleCache = articleCache ?? throw new ArgumentNullException(nameof(articleCache));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Article CRUD operations

    public async Task<ArticleDto?> GetArticleAsync(Guid articleId)
    {
        return await _articleApi.GetArticleAsync(articleId);
    }

    public async Task<ArticleDto?> UpdateArticleAsync(Guid articleId, ArticleUpdateDto updateDto)
    {
        return await _articleApi.UpdateArticleAsync(articleId, updateDto);
    }

    public async Task<bool> DeleteArticleAsync(Guid articleId)
    {
        return await _articleApi.DeleteArticleAsync(articleId);
    }

    public async Task<ArticleDto?> CreateArticleAsync(ArticleCreateDto createDto)
    {
        return await _articleApi.CreateArticleAsync(createDto);
    }

    // Link operations

    public async Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId)
    {
        return await _linkApi.GetBacklinksAsync(articleId);
    }

    public async Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId)
    {
        return await _linkApi.GetOutgoingLinksAsync(articleId);
    }

    public async Task<AutoLinkResponseDto?> AutoLinkAsync(Guid articleId, string markdown)
    {
        return await _linkApi.AutoLinkAsync(articleId, markdown);
    }

    // External link operations

    public async Task<ExternalLinkContentDto?> GetExternalLinkContentAsync(string source, string id, CancellationToken ct)
    {
        return await _externalLinkApi.GetContentAsync(source, id, ct);
    }

    public async Task<List<ExternalLinkSuggestionDto>> GetExternalLinkSuggestionsAsync(Guid? worldId, string source, string query, CancellationToken ct)
    {
        return await _externalLinkApi.GetSuggestionsAsync(worldId, source, query, ct);
    }

    // Wiki link operations

    public async Task<ArticleDto?> CreateArticleFromWikiLinkAsync(string articleName, Guid worldId)
    {
        return await _wikiLinkService.CreateArticleFromAutocompleteAsync(articleName, worldId);
    }

    // Markdown operations

    public string RenderMarkdownToHtml(string markdown)
    {
        return _markdownService.ToHtml(markdown);
    }

    // Navigation & state operations

    public void NavigateToArticle(string path)
    {
        _navigationManager.NavigateTo(path);
    }

    public string BuildArticleUrlFromBreadcrumbs(List<BreadcrumbDto> breadcrumbs)
    {
        return _breadcrumbService.BuildArticleUrl(breadcrumbs);
    }

    public async Task SelectArticleInTreeAsync(Guid articleId)
    {
        _treeState.SelectNode(articleId);
        await Task.CompletedTask;
    }

    public async Task RefreshTreeAsync()
    {
        await _treeState.RefreshAsync();
    }

    public Guid? GetCurrentWorldId()
    {
        return _appContext.CurrentWorldId;
    }

    // UI interaction operations

    public void ShowSuccessNotification(string message)
    {
        _snackbar.Add(message, Severity.Success);
    }

    public void ShowErrorNotification(string message)
    {
        _snackbar.Add(message, Severity.Error);
    }

    public void ToggleMetadataDrawer()
    {
        _metadataDrawerService.Toggle();
    }

    public void TriggerSaveShortcut()
    {
        _keyboardShortcutService.RequestSave();
    }

    // Cache & storage operations

    public async Task<string?> GetArticleNavigationPathAsync(Guid articleId)
    {
        return await _articleCache.GetNavigationPathAsync(articleId);
    }

    public void CacheArticle(ArticleDto article)
    {
        _articleCache.CacheArticle(article);
    }

    public async Task<T?> GetFromLocalStorageAsync<T>(string key)
    {
        return await _localStorage.GetItemAsync<T>(key);
    }

    public async Task SetInLocalStorageAsync<T>(string key, T value)
    {
        await _localStorage.SetItemAsync(key, value);
    }

    // JavaScript interop & logging operations

    public async Task<IJSObjectReference?> ImportJavaScriptModuleAsync(string modulePath)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import JavaScript module: {ModulePath}", modulePath);
            return null;
        }
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogError(Exception ex, string message)
    {
        _logger.LogError(ex, message);
    }
}
