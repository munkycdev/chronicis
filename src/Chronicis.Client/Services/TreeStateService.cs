using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public class TreeStateService : ITreeStateService
{
    private readonly IArticleApiService _apiService;

    private List<ArticleTreeItemViewModel> _rootItems = new();
    private ArticleTreeItemViewModel? _selectedArticle;
    private string _searchQuery = string.Empty;
    private List<ArticleSearchResultDto> _searchResults = new();
    private HashSet<int> _visibleNodeIds = new();

    public event Action? OnRefreshRequested;
    public event Action<int>? OnExpandAndSelect;
    public event Action? OnStateChanged;

    public int? SelectedArticleId { get; private set; }
    public List<ArticleTreeItemViewModel> RootItems => _rootItems;
    public ArticleTreeItemViewModel? SelectedArticle => _selectedArticle;
    public string SearchQuery => _searchQuery;
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(_searchQuery);

    public TreeStateService(IArticleApiService apiService)
    {
        _apiService = apiService;
    }

    public void NotifySelectionChanged(int articleId)
    {
        SelectedArticleId = articleId;
        NotifyStateChanged();
    }

    public void ExpandAndSelectArticle(int articleId)
    {
        SelectedArticleId = articleId;
        OnExpandAndSelect?.Invoke(articleId);
        NotifyStateChanged();
    }

    public void Initialize(List<ArticleTreeDto> rootArticles)
    {
        _rootItems = rootArticles.Select(MapToTreeViewModel).ToList();
        NotifyStateChanged();
    }

    public void LoadChildren(ArticleTreeItemViewModel parent, List<ArticleTreeDto> children)
    {
        parent.Children = children.Select(c =>
        {
            var vm = MapToTreeViewModel(c);
            vm.ParentId = parent.Id;
            return vm;
        }).ToList();

        parent.IsExpanded = true;
        NotifyStateChanged();
    }

    public void SelectArticle(int articleId)
    {
        if (_selectedArticle != null)
        {
            _selectedArticle.IsSelected = false;
        }

        var article = FindArticleById(_rootItems, articleId);
        if (article != null)
        {
            _selectedArticle = article;
            article.IsSelected = true;
        }

        NotifyStateChanged();
    }

    public void AddArticle(ArticleDto article)
    {
        var viewModel = MapToViewModel(article);

        if (article.ParentId.HasValue)
        {
            var parent = FindArticleById(_rootItems, article.ParentId.Value);
            if (parent != null)
            {
                viewModel.ParentId = parent.Id;
                parent.Children.Add(viewModel);
                parent.IsExpanded = true;
            }
        }
        else
        {
            _rootItems.Add(viewModel);
        }

        SelectArticle(viewModel.Id);
        NotifyStateChanged();
    }

    public void UpdateArticle(ArticleDto article)
    {
        var existing = FindArticleById(_rootItems, article.Id);
        if (existing != null)
        {
            existing.Title = article.Title;
            existing.Body = article.Body;

            SelectArticle(existing.Id);
            NotifyStateChanged();
        }
    }

    public void RemoveArticle(int articleId)
    {
        if (RemoveArticleRecursive(_rootItems, articleId))
        {
            if (_selectedArticle?.Id == articleId)
            {
                _selectedArticle = null;
            }
            NotifyStateChanged();
        }
    }

    public async Task SearchAsync(string query)
    {
        _searchQuery = query ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            ClearSearch();
            return;
        }

        _searchResults = await _apiService.SearchArticlesByTitleAsync(_searchQuery);

        _visibleNodeIds.Clear();
        foreach (var result in _searchResults)
        {
            foreach (var ancestor in result.AncestorPath)
            {
                _visibleNodeIds.Add(ancestor.Id);
            }
        }

        foreach (var result in _searchResults)
        {
            for (int i = 0; i < result.AncestorPath.Count - 1; i++)
            {
                var ancestorId = result.AncestorPath[i].Id;
                var ancestor = FindArticleById(_rootItems, ancestorId);
                if (ancestor != null && !ancestor.IsExpanded)
                {
                    ancestor.IsExpanded = true;
                }
            }
        }

        NotifyStateChanged();
    }

    public void ClearSearch()
    {
        _searchQuery = string.Empty;
        _searchResults.Clear();
        _visibleNodeIds.Clear();
        NotifyStateChanged();
    }

    public bool IsNodeVisible(int articleId)
    {
        if (!IsSearchActive)
            return true;

        return _visibleNodeIds.Contains(articleId);
    }

    public void RefreshTree()
    {
        OnRefreshRequested?.Invoke();
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

    private ArticleTreeItemViewModel MapToTreeViewModel(ArticleTreeDto dto)
    {
        return new ArticleTreeItemViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            ParentId = dto.ParentId,
            HasChildren = dto.HasChildren,
            Children = dto.Children?.Select(c => MapToTreeViewModel(c)).ToList()
                ?? new List<ArticleTreeItemViewModel>(),
            IsExpanded = false,
            IsSelected = false
        };
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
            Children = dto.Children?.Select(c => MapToViewModel(c)).ToList()
                ?? new List<ArticleTreeItemViewModel>(),
            IsExpanded = false,
            IsSelected = false
        };
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
