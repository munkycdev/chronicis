using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// ViewModel interface for ArticleDetail component.
/// Separates presentation logic from UI rendering for improved testability.
/// </summary>
public interface IArticleDetailViewModel
{
    // State properties
    ArticleDto? Article { get; }
    bool IsLoading { get; }
    bool IsSaving { get; }
    bool IsDeleting { get; }
    string? ErrorMessage { get; }
    string? SuccessMessage { get; }
    
    // Edit state
    string? EditTitle { get; }
    string? EditBody { get; }
    DateTime? EditEffectiveDate { get; }
    TimeSpan? EditEffectiveTime { get; }
    
    // UI state
    bool IsEditMode { get; }
    bool ShowMetadataDrawer { get; }
    
    // Events for UI updates
    event Action? OnStateChanged;
    
    // Lifecycle methods
    Task LoadArticleAsync(Guid articleId);
    void StartEdit();
    void CancelEdit();
    
    // CRUD operations
    Task SaveArticleAsync();
    Task DeleteArticleAsync();
    Task CreateChildArticleAsync(string title);
    
    // UI interactions
    void UpdateEditTitle(string title);
    void UpdateEditBody(string body);
    void UpdateEditEffectiveDate(DateTime? date);
    void UpdateEditEffectiveTime(TimeSpan? time);
    void ToggleMetadataDrawer();
    
    // Navigation
    void NavigateToParent();
    void NavigateToArticle(Guid articleId);
}
