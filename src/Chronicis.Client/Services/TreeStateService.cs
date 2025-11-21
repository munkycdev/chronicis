using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;

namespace Chronicis.Client.Services;

public class TreeStateService
{
    private List<ArticleTreeItemViewModel> _rootItems = new();
    private ArticleTreeItemViewModel? _selectedArticle;

    public event Action? OnStateChanged;

    public List<ArticleTreeItemViewModel> RootItems => _rootItems;
    public ArticleTreeItemViewModel? SelectedArticle => _selectedArticle;

    public void Initialize(List<ArticleDto> rootArticles)
    {
        _rootItems = rootArticles.Select(MapToViewModel).ToList();
        NotifyStateChanged();
    }

    public async Task LoadChildrenAsync(ArticleTreeItemViewModel parent, List<ArticleDto> children)
    {
        parent.Children = children.Select(c =>
        {
            var vm = MapToViewModel(c);
            vm.ParentId = parent.Id;
            return vm;
        }).ToList();

        parent.IsExpanded = true;
        NotifyStateChanged();
    }

    public void SelectArticle(ArticleTreeItemViewModel article)
    {
        // Deselect previous
        if (_selectedArticle != null)
        {
            _selectedArticle.IsSelected = false;
        }

        // Select new
        _selectedArticle = article;
        article.IsSelected = true;
        NotifyStateChanged();
    }

    // NEW METHODS FOR PHASE 2
    public async Task AddArticleAsync(ArticleDto article)
    {
        var viewModel = MapToViewModel(article);

        if (article.ParentId.HasValue)
        {
            // Find parent and add as child
            var parent = FindArticleById(_rootItems, article.ParentId.Value);
            if (parent != null)
            {
                viewModel.ParentId = parent.Id;
                parent.Children.Add(viewModel);
                parent.IsExpanded = true; // Auto-expand to show new child
            }
        }
        else
        {
            // Add as root article
            _rootItems.Add(viewModel);
        }

        NotifyStateChanged();
    }

    public async Task UpdateArticleAsync(ArticleDto article)
    {
        var existing = FindArticleById(_rootItems, article.Id);
        if (existing != null)
        {
            existing.Title = article.Title;
            existing.Body = article.Body;
            NotifyStateChanged();
        }
    }

    public async Task RemoveArticleAsync(int articleId)
    {
        if (RemoveArticleRecursive(_rootItems, articleId))
        {
            // If we deleted the selected article, clear selection
            if (_selectedArticle?.Id == articleId)
            {
                _selectedArticle = null;
            }
            NotifyStateChanged();
        }
    }

    private bool RemoveArticleRecursive(List<ArticleTreeItemViewModel> items, int articleId)
    {
        var item = items.FirstOrDefault(i => i.Id == articleId);
        if (item != null)
        {
            items.Remove(item);
            return true;
        }

        foreach (var childItem in items)
        {
            if (RemoveArticleRecursive(childItem.Children, articleId))
            {
                return true;
            }
        }

        return false;
    }

    private ArticleTreeItemViewModel? FindArticleById(List<ArticleTreeItemViewModel> items, int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
                return item;

            var found = FindArticleById(item.Children, id);
            if (found != null)
                return found;
        }

        return null;
    }

    private ArticleTreeItemViewModel MapToViewModel(ArticleDto dto)
    {
        return new ArticleTreeItemViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            ParentId = dto.ParentId,
            Body = dto.Body,
            HasChildren = dto.HasChildren,
            Children = dto.Children?.Select(c => MapToViewModel(c)).ToList() ?? new List<ArticleTreeItemViewModel>(),  // Add recursive mapping
            IsExpanded = false,
            IsSelected = false
        };
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
