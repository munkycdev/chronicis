using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels.ArticleDetail;

/// <summary>
/// Manages edit mode state and edit field values.
/// </summary>
public class ArticleEditState
{
    public bool IsEditMode { get; private set; }
    public string? EditTitle { get; private set; }
    public string? EditBody { get; private set; }
    public DateTime? EditEffectiveDate { get; private set; }
    public TimeSpan? EditEffectiveTime { get; private set; }
    
    public event Action? OnStateChanged;
    
    public void StartEdit(ArticleDto article)
    {
        IsEditMode = true;
        EditTitle = article.Title;
        EditBody = article.Body;
        EditEffectiveDate = article.EffectiveDate;
        EditEffectiveTime = article.EffectiveDate.TimeOfDay;
        NotifyStateChanged();
    }
    
    public void CancelEdit()
    {
        IsEditMode = false;
        EditTitle = null;
        EditBody = null;
        EditEffectiveDate = null;
        EditEffectiveTime = null;
        NotifyStateChanged();
    }
    
    public void ExitEditMode()
    {
        IsEditMode = false;
        NotifyStateChanged();
    }
    
    public void UpdateTitle(string title)
    {
        EditTitle = title;
        NotifyStateChanged();
    }
    
    public void UpdateBody(string body)
    {
        EditBody = body;
        NotifyStateChanged();
    }
    
    public void UpdateEffectiveDate(DateTime? date)
    {
        EditEffectiveDate = date;
        NotifyStateChanged();
    }
    
    public void UpdateEffectiveTime(TimeSpan? time)
    {
        EditEffectiveTime = time;
        NotifyStateChanged();
    }
    
    public ArticleUpdateDto CreateUpdateDto(ArticleDto currentArticle)
    {
        return new ArticleUpdateDto
        {
            Title = EditTitle ?? currentArticle.Title,
            Body = EditBody ?? currentArticle.Body,
            EffectiveDate = CombineDateAndTime(
                EditEffectiveDate ?? currentArticle.EffectiveDate, 
                EditEffectiveTime)
        };
    }
    
    private static DateTime CombineDateAndTime(DateTime date, TimeSpan? time)
    {
        if (time.HasValue)
        {
            return date.Date.Add(time.Value);
        }
        return date;
    }
    
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
