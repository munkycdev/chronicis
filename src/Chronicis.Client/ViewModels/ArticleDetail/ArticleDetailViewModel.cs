using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// Main ViewModel for ArticleDetail component.
/// Coordinates smaller, focused state management components.
/// </summary>
public class ArticleDetailViewModel : IArticleDetailViewModel
{
    private readonly IArticleDetailFacade _facade;
    private readonly ArticleLoadingState _loadingState;
    private readonly ArticleEditState _editState;
    private readonly ArticleOperations _operations;
    private bool _showMetadataDrawer;
    
    public ArticleDetailViewModel(IArticleDetailFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        
        // Create sub-components
        _loadingState = new ArticleLoadingState();
        _editState = new ArticleEditState();
        _operations = new ArticleOperations(facade, _loadingState, _editState);
        
        // Subscribe to sub-component state changes
        _loadingState.OnStateChanged += NotifyStateChanged;
        _editState.OnStateChanged += NotifyStateChanged;
        _operations.OnStateChanged += NotifyStateChanged;
    }
    
    // Expose state from sub-components
    public ArticleDto? Article => _loadingState.Article;
    public bool IsLoading => _loadingState.IsLoading;
    public bool IsSaving => _operations.IsSaving;
    public bool IsDeleting => _operations.IsDeleting;
    public string? ErrorMessage => _loadingState.ErrorMessage;
    public string? SuccessMessage => _loadingState.SuccessMessage;
    public string? EditTitle => _editState.EditTitle;
    public string? EditBody => _editState.EditBody;
    public DateTime? EditEffectiveDate => _editState.EditEffectiveDate;
    public TimeSpan? EditEffectiveTime => _editState.EditEffectiveTime;
    public bool IsEditMode => _editState.IsEditMode;
    public bool ShowMetadataDrawer => _showMetadataDrawer;
    
    public event Action? OnStateChanged;
    
    // Lifecycle methods
    public async Task LoadArticleAsync(Guid articleId)
    {
        _loadingState.SetLoading(true);
        _loadingState.ClearMessages();
        
        try
        {
            var article = await _facade.GetArticleAsync(articleId);
            
            if (article == null)
            {
                _loadingState.SetError("Article not found");
            }
            else
            {
                _loadingState.SetArticle(article);
                _facade.CacheArticle(article);
                await _facade.SelectArticleInTreeAsync(articleId);
            }
        }
        catch (Exception ex)
        {
            _loadingState.SetError($"Failed to load article: {ex.Message}");
            _facade.LogError(ex, "Failed to load article");
        }
        finally
        {
            _loadingState.SetLoading(false);
        }
    }
    
    public void StartEdit()
    {
        if (Article != null)
        {
            _editState.StartEdit(Article);
        }
    }
    
    public void CancelEdit()
    {
        _editState.CancelEdit();
    }
    
    // Delegate CRUD operations
    public Task SaveArticleAsync() => _operations.SaveArticleAsync();
    public Task DeleteArticleAsync() => _operations.DeleteArticleAsync();
    public Task CreateChildArticleAsync(string title) => _operations.CreateChildArticleAsync(title);
    
    // Delegate edit updates
    public void UpdateEditTitle(string title) => _editState.UpdateTitle(title);
    public void UpdateEditBody(string body) => _editState.UpdateBody(body);
    public void UpdateEditEffectiveDate(DateTime? date) => _editState.UpdateEffectiveDate(date);
    public void UpdateEditEffectiveTime(TimeSpan? time) => _editState.UpdateEffectiveTime(time);
    
    // UI interactions
    public void ToggleMetadataDrawer()
    {
        _showMetadataDrawer = !_showMetadataDrawer;
        _facade.ToggleMetadataDrawer();
        NotifyStateChanged();
    }
    
    // Navigation
    public void NavigateToParent()
    {
        if (Article?.ParentId != null)
        {
            NavigateToArticle(Article.ParentId.Value);
        }
    }
    
    public async void NavigateToArticle(Guid articleId)
    {
        var path = await _facade.GetArticleNavigationPathAsync(articleId);
        if (path != null)
        {
            _facade.NavigateToArticle(path);
        }
    }
    
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
