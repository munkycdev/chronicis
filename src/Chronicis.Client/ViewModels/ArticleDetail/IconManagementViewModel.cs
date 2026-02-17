using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// ViewModel for managing article icon updates.
/// Handles icon selection, updates, and notifications.
/// </summary>
public class IconManagementViewModel
{
    private readonly IArticleDetailFacade _facade;

    public IconManagementViewModel(IArticleDetailFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    // State
    public string? CurrentIcon { get; private set; }
    public bool IsSaving { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Events
    public event Action? OnStateChanged;
    public event Action<string?>? OnIconUpdated;

    /// <summary>
    /// Updates the article icon.
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="currentTitle">Current article title</param>
    /// <param name="currentBody">Current article body</param>
    /// <param name="currentEffectiveDate">Current effective date</param>
    /// <param name="newIcon">New icon emoji (null to clear)</param>
    public async Task UpdateIconAsync(
        Guid articleId,
        string currentTitle,
        string currentBody,
        DateTime currentEffectiveDate,
        string? newIcon)
    {
        IsSaving = true;
        ErrorMessage = null;
        OnStateChanged?.Invoke();

        try
        {
            var updateDto = new ArticleUpdateDto
            {
                Title = currentTitle,
                Body = currentBody,
                EffectiveDate = currentEffectiveDate,
                IconEmoji = newIcon
            };

            var updatedArticle = await _facade.UpdateArticleAsync(articleId, updateDto);
            
            if (updatedArticle != null)
            {
                CurrentIcon = newIcon;
                OnIconUpdated?.Invoke(newIcon);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Clears any error message.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
        OnStateChanged?.Invoke();
    }
}
