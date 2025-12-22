using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface ITreeStateService
    {
        void ExpandAndSelectArticle(Guid articleId);
        event Action<Guid>? OnExpandAndSelect;
        void NotifySelectionChanged(Guid articleId);
        Guid? SelectedArticleId { get; }
        bool IsSearchActive { get; }
        List<ArticleTreeItemViewModel> RootItems { get; }
        string SearchQuery { get; }
        ArticleTreeItemViewModel? SelectedArticle { get; }
        
        /// <summary>
        /// Flag to indicate the title field should be focused when loading an article
        /// </summary>
        bool ShouldFocusTitle { get; set; }

        event Action? OnStateChanged;
        event Action? OnRefreshRequested;

        void AddArticle(ArticleDto article);
        void ClearSearch();
        void Initialize(List<ArticleTreeDto> rootArticles);
        bool IsNodeVisible(Guid articleId);
        void LoadChildren(ArticleTreeItemViewModel parent, List<ArticleTreeDto> children);
        void RemoveArticle(Guid articleId);
        void RefreshTree();
        Task SearchAsync(string query);
        void SelectArticle(Guid articleId);
        void UpdateArticle(ArticleDto article);
    }
}
