using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface ITreeStateService
    {
        void ExpandAndSelectArticle(int articleId);
        event Action<int>? OnExpandAndSelect;
        void NotifySelectionChanged(int articleId);
        int? SelectedArticleId { get; }
        bool IsSearchActive { get; }
        List<ArticleTreeItemViewModel> RootItems { get; }
        string SearchQuery { get; }
        ArticleTreeItemViewModel? SelectedArticle { get; }

        event Action? OnStateChanged;
        event Action? OnRefreshRequested;

        void AddArticle(ArticleDto article);
        void ClearSearch();
        void Initialize(List<ArticleTreeDto> rootArticles);
        bool IsNodeVisible(int articleId);
        void LoadChildren(ArticleTreeItemViewModel parent, List<ArticleTreeDto> children);
        void RemoveArticle(int articleId);
        void RefreshTree();
        Task SearchAsync(string query);
        void SelectArticle(int articleId);
        void UpdateArticle(ArticleDto article);
    }
}
