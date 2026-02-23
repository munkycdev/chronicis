using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the Search page.
/// Handles content search, result state, and navigation to matched articles.
/// </summary>
public sealed class SearchViewModel : ViewModelBase
{
    private readonly ISearchApiService _searchApi;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly ITreeStateService _treeState;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<SearchViewModel> _logger;

    private bool _isLoading;
    private GlobalSearchResultsDto? _results;

    public SearchViewModel(
        ISearchApiService searchApi,
        IBreadcrumbService breadcrumbService,
        ITreeStateService treeState,
        IAppNavigator navigator,
        ILogger<SearchViewModel> logger)
    {
        _searchApi = searchApi;
        _breadcrumbService = breadcrumbService;
        _treeState = treeState;
        _navigator = navigator;
        _logger = logger;
    }

    /// <summary>Whether a search request is in flight.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>The results of the most recent search, or <c>null</c> if no search has been performed.</summary>
    public GlobalSearchResultsDto? Results
    {
        get => _results;
        private set => SetField(ref _results, value);
    }

    /// <summary>
    /// Executes a content search for the given query.
    /// Clears results and does nothing when <paramref name="query"/> is null or whitespace.
    /// </summary>
    public async Task SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Results = null;
            return;
        }

        IsLoading = true;

        try
        {
            Results = await _searchApi.SearchContentAsync(query);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Search error for query: {Query}", query);
            Results = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Expands the tree to the article and navigates to it.
    /// </summary>
    public void NavigateToArticle(ArticleSearchResultDto result)
    {
        _treeState.ExpandPathToAndSelect(result.Id);

        if (result.AncestorPath != null && result.AncestorPath.Any())
        {
            var path = _breadcrumbService.BuildArticleUrl(result.AncestorPath);
            _navigator.NavigateTo(path);
        }
        else
        {
            _navigator.NavigateTo($"/article/{result.Slug}");
        }
    }
}
