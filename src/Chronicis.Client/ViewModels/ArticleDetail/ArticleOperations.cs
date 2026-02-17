using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// Handles article CRUD operations (save, delete, create child).
/// </summary>
public class ArticleOperations
{
    private readonly IArticleDetailFacade _facade;
    private readonly ArticleLoadingState _loadingState;
    private readonly ArticleEditState _editState;
    
    public bool IsSaving { get; private set; }
    public bool IsDeleting { get; private set; }
    
    public event Action? OnStateChanged;
    
    public ArticleOperations(
        IArticleDetailFacade facade, 
        ArticleLoadingState loadingState,
        ArticleEditState editState)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _loadingState = loadingState ?? throw new ArgumentNullException(nameof(loadingState));
        _editState = editState ?? throw new ArgumentNullException(nameof(editState));
    }
    
    public async Task SaveArticleAsync()
    {
        var article = _loadingState.Article;
        if (article == null || !_editState.IsEditMode) return;
        
        IsSaving = true;
        _loadingState.ClearMessages();
        NotifyStateChanged();
        
        try
        {
            var updateDto = _editState.CreateUpdateDto(article);
            var updated = await _facade.UpdateArticleAsync(article.Id, updateDto);
            
            if (updated != null)
            {
                _loadingState.SetArticle(updated);
                _facade.CacheArticle(updated);
                _editState.ExitEditMode();
                _loadingState.SetSuccess("Article saved successfully");
                _facade.ShowSuccessNotification("Article saved");
                await _facade.RefreshTreeAsync();
            }
            else
            {
                _loadingState.SetError("Failed to save article");
                _facade.ShowErrorNotification("Failed to save article");
            }
        }
        catch (Exception ex)
        {
            _loadingState.SetError($"Error saving article: {ex.Message}");
            _facade.LogError(ex, "Error saving article");
            _facade.ShowErrorNotification("Error saving article");
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }
    
    public async Task DeleteArticleAsync()
    {
        var article = _loadingState.Article;
        if (article == null) return;
        
        IsDeleting = true;
        _loadingState.ClearMessages();
        NotifyStateChanged();
        
        try
        {
            var success = await _facade.DeleteArticleAsync(article.Id);
            
            if (success)
            {
                _facade.ShowSuccessNotification("Article deleted");
                
                // Navigate to parent or world root
                if (article.ParentId.HasValue)
                {
                    var path = await _facade.GetArticleNavigationPathAsync(article.ParentId.Value);
                    if (path != null) _facade.NavigateToArticle(path);
                }
                else
                {
                    var worldId = _facade.GetCurrentWorldId();
                    if (worldId.HasValue)
                    {
                        _facade.NavigateToArticle($"/worlds/{worldId}");
                    }
                }
                
                await _facade.RefreshTreeAsync();
            }
            else
            {
                _loadingState.SetError("Failed to delete article");
                _facade.ShowErrorNotification("Failed to delete article");
            }
        }
        catch (Exception ex)
        {
            _loadingState.SetError($"Error deleting article: {ex.Message}");
            _facade.LogError(ex, "Error deleting article");
            _facade.ShowErrorNotification("Error deleting article");
        }
        finally
        {
            IsDeleting = false;
            NotifyStateChanged();
        }
    }
    
    public async Task CreateChildArticleAsync(string title)
    {
        var article = _loadingState.Article;
        if (article == null || string.IsNullOrWhiteSpace(title)) return;
        
        IsSaving = true;
        _loadingState.ClearMessages();
        NotifyStateChanged();
        
        try
        {
            var createDto = new ArticleCreateDto
            {
                Title = title,
                Body = string.Empty,
                ParentId = article.Id,
                EffectiveDate = DateTime.Now,
                WorldId = article.WorldId
            };
            
            var created = await _facade.CreateArticleAsync(createDto);
            
            if (created != null)
            {
                _facade.ShowSuccessNotification("Child article created");
                
                if (created.Breadcrumbs != null && created.Breadcrumbs.Any())
                {
                    var path = _facade.BuildArticleUrlFromBreadcrumbs(created.Breadcrumbs);
                    _facade.NavigateToArticle(path);
                }
                
                await _facade.RefreshTreeAsync();
            }
            else
            {
                _loadingState.SetError("Failed to create child article");
                _facade.ShowErrorNotification("Failed to create child article");
            }
        }
        catch (Exception ex)
        {
            _loadingState.SetError($"Error creating child article: {ex.Message}");
            _facade.LogError(ex, "Error creating child article");
            _facade.ShowErrorNotification("Error creating child article");
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }
    
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
