using Chronicis.Shared.DTOs;
using Microsoft.JSInterop;

namespace Chronicis.Client.Services;

/// <summary>
/// Facade that wraps 16 services used by ArticleDetail component.
/// This simplifies testing and component dependencies by providing a single point of interaction.
/// 
/// Services wrapped (16 total):
/// 1. IArticleApiService - Article CRUD operations
/// 2. ILinkApiService - Backlinks and outgoing links
/// 3. IExternalLinkApiService - External link content and suggestions  
/// 4. IWikiLinkService - Wiki link creation
/// 5. IMarkdownService - Markdown to HTML rendering
/// 6. NavigationManager - Page navigation
/// 7. IBreadcrumbService - Breadcrumb URL building
/// 8. ITreeStateService - Tree state management
/// 9. IAppContextService - Current world/campaign context
/// 10. ISnackbar - User notifications
/// 11. IMetadataDrawerService - Metadata drawer toggle
/// 12. IKeyboardShortcutService - Keyboard shortcut triggers
/// 13. IArticleCacheService - Article metadata caching
/// 14. ILocalStorageService - Browser local storage
/// 15. IJSRuntime - JavaScript interop
/// 16. ILogger&lt;ArticleDetailFacade&gt; - Logging
/// </summary>
public interface IArticleDetailFacade
{
    // Article CRUD operations
    Task<ArticleDto?> GetArticleAsync(Guid articleId);
    Task<ArticleDto?> UpdateArticleAsync(Guid articleId, ArticleUpdateDto updateDto);
    Task<bool> DeleteArticleAsync(Guid articleId);
    Task<ArticleDto?> CreateArticleAsync(ArticleCreateDto createDto);

    // Link operations
    Task<List<BacklinkDto>> GetBacklinksAsync(Guid articleId);
    Task<List<BacklinkDto>> GetOutgoingLinksAsync(Guid articleId);
    Task<AutoLinkResponseDto?> AutoLinkAsync(Guid articleId, string markdown);
    
    // External link operations
    Task<ExternalLinkContentDto?> GetExternalLinkContentAsync(string source, string id, CancellationToken ct);
    Task<List<ExternalLinkSuggestionDto>> GetExternalLinkSuggestionsAsync(Guid? worldId, string source, string query, CancellationToken ct);
    
    // Wiki link operations
    Task<ArticleDto?> CreateArticleFromWikiLinkAsync(string articleName, Guid worldId);
    
    // Markdown operations
    string RenderMarkdownToHtml(string markdown);
    
    // Navigation & state operations
    void NavigateToArticle(string path);
    string BuildArticleUrlFromBreadcrumbs(List<BreadcrumbDto> breadcrumbs);
    Task SelectArticleInTreeAsync(Guid articleId);
    Task RefreshTreeAsync();
    Guid? GetCurrentWorldId();
    
    // UI interaction operations
    void ShowSuccessNotification(string message);
    void ShowErrorNotification(string message);
    void ToggleMetadataDrawer();
    void TriggerSaveShortcut();
    
    // Cache & storage operations
    Task<string?> GetArticleNavigationPathAsync(Guid articleId);
    void CacheArticle(ArticleDto article);
    Task<T?> GetFromLocalStorageAsync<T>(string key);
    Task SetInLocalStorageAsync<T>(string key, T value);
    
    // JavaScript interop & logging operations
    Task<IJSObjectReference?> ImportJavaScriptModuleAsync(string modulePath);
    void LogInformation(string message);
    void LogError(Exception ex, string message);
}
