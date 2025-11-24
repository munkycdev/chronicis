using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    public interface ITreeStateService
    {
        bool IsSearchActive { get; }
        List<ArticleTreeItemViewModel> RootItems { get; }
        string SearchQuery { get; }
        ArticleTreeItemViewModel? SelectedArticle { get; }

        event Action? OnStateChanged;
        event Action? OnRefreshRequested;

        Task AddArticleAsync(ArticleDto article);
        void ClearSearch();
        void Initialize(List<ArticleTreeDto> rootArticles);
        bool IsNodeVisible(int articleId);
        Task LoadChildrenAsync(ArticleTreeItemViewModel parent, List<ArticleTreeDto> children);
        Task RemoveArticleAsync(int articleId);
        void RefreshTree();
        Task SearchAsync(string query);
        void SelectArticle(int articleId);  // Changed to just ID
        Task UpdateArticleAsync(ArticleDto article);
    }
}