using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// Manages loading state and current article data.
/// </summary>
public class ArticleLoadingState
{
    public ArticleDto? Article { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    
    public event Action? OnStateChanged;
    
    public void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
        NotifyStateChanged();
    }
    
    public void SetArticle(ArticleDto? article)
    {
        Article = article;
        NotifyStateChanged();
    }
    
    public void SetError(string? errorMessage)
    {
        ErrorMessage = errorMessage;
        NotifyStateChanged();
    }
    
    public void SetSuccess(string? successMessage)
    {
        SuccessMessage = successMessage;
        NotifyStateChanged();
    }
    
    public void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }
    
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
