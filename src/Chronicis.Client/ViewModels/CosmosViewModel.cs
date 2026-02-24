using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the Cosmos page (the welcome/dashboard shown when no article is selected).
/// Owns data loading, stat calculation, quote rotation, and article navigation.
/// Implements <see cref="IDisposable"/> to clean up the tree-state subscription.
/// </summary>
public sealed class CosmosViewModel : ViewModelBase, IDisposable
{
    private readonly IArticleApiService _articleApi;
    private readonly IQuoteService _quoteService;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly ITreeStateService _treeState;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<CosmosViewModel> _logger;

    private CampaignStats? _stats;
    private List<ArticleDto>? _recentArticles;
    private bool _isLoadingRecent = true;
    private Quote? _quote;
    private bool _loadingQuote = true;

    /// <summary>Total articles count and related stats, or <c>null</c> before first load.</summary>
    public CampaignStats? Stats
    {
        get => _stats;
        private set => SetField(ref _stats, value);
    }

    /// <summary>The five most-recently-modified articles across the entire tree.</summary>
    public List<ArticleDto>? RecentArticles
    {
        get => _recentArticles;
        private set => SetField(ref _recentArticles, value);
    }

    /// <summary>Whether the recent-articles section is currently loading.</summary>
    public bool IsLoadingRecent
    {
        get => _isLoadingRecent;
        private set => SetField(ref _isLoadingRecent, value);
    }

    /// <summary>The current inspirational quote, or <c>null</c> before first load.</summary>
    public Quote? Quote
    {
        get => _quote;
        private set => SetField(ref _quote, value);
    }

    /// <summary>Whether the quote section is currently loading.</summary>
    public bool LoadingQuote
    {
        get => _loadingQuote;
        private set => SetField(ref _loadingQuote, value);
    }

    public CosmosViewModel(
        IArticleApiService articleApi,
        IQuoteService quoteService,
        IBreadcrumbService breadcrumbService,
        ITreeStateService treeState,
        IAppNavigator navigator,
        ILogger<CosmosViewModel> logger)
    {
        _articleApi = articleApi;
        _quoteService = quoteService;
        _breadcrumbService = breadcrumbService;
        _treeState = treeState;
        _navigator = navigator;
        _logger = logger;

        _treeState.OnStateChanged += OnTreeStateChanged;
    }

    /// <summary>
    /// Loads dashboard data and quote in parallel, and optionally navigates to an article by URL path.
    /// Call from <c>OnInitializedAsync</c>.
    /// </summary>
    public async Task InitializeAsync(string? path)
    {
        if (!string.IsNullOrEmpty(path))
            await LoadArticleByPathAsync(path);

        await Task.WhenAll(LoadDashboardDataAsync(), LoadQuoteAsync());
    }

    /// <summary>
    /// Handles URL changes (browser back/forward). Call from <c>OnParametersSetAsync</c>.
    /// </summary>
    public async Task OnParametersSetAsync(string? path)
    {
        if (!string.IsNullOrEmpty(path))
            await LoadArticleByPathAsync(path);
    }

    /// <summary>Refreshes the inspirational quote.</summary>
    public async Task LoadNewQuoteAsync() => await LoadQuoteAsync();

    /// <summary>Creates an article with an empty title (the "Start Your First Article" hero button).</summary>
    public async Task CreateFirstArticleAsync() => await CreateArticleWithTitleAsync(string.Empty);

    /// <summary>Creates a new root article pre-populated with <paramref name="title"/> and navigates to it.</summary>
    public async Task CreateArticleWithTitleAsync(string title)
    {
        var createDto = new ArticleCreateDto
        {
            Title = title,
            Body = string.Empty,
            ParentId = null,
            EffectiveDate = DateTime.Now,
        };

        var created = await _articleApi.CreateArticleAsync(createDto);
        if (created == null)
        {
            _logger.LogErrorSanitized("Failed to create article");
            return;
        }

        await _treeState.RefreshAsync();
        _treeState.ExpandPathToAndSelect(created.Id);
    }

    /// <summary>Navigates to <paramref name="articleId"/> by resolving its breadcrumb path.</summary>
    public async Task NavigateToArticleAsync(Guid articleId)
    {
        var article = await _articleApi.GetArticleDetailAsync(articleId);
        if (article != null && article.Breadcrumbs.Any())
        {
            var path = _breadcrumbService.BuildArticleUrl(article.Breadcrumbs);
            _navigator.NavigateTo(path);
        }
    }

    /// <summary>
    /// Formats a <see cref="DateTime"/> as a human-readable relative string (e.g. "3h ago").
    /// </summary>
    public static string FormatRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task LoadArticleByPathAsync(string path)
    {
        try
        {
            var article = await _articleApi.GetArticleByPathAsync(path);

            if (article != null)
            {
                if (article.Id != _treeState.SelectedArticleId)
                    _treeState.ExpandPathToAndSelect(article.Id);
            }
            else if (!_treeState.SelectedArticleId.HasValue)
            {
                _logger.LogWarningSanitized("Article not found for path: {Path}", path);
                _navigator.NavigateTo("/", replace: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading article by path: {Path}", path);
            if (!_treeState.SelectedArticleId.HasValue)
                _navigator.NavigateTo("/", replace: true);
        }
    }

    private async Task LoadQuoteAsync()
    {
        LoadingQuote = true;
        try
        {
            Quote = await _quoteService.GetRandomQuoteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading quote");
        }
        finally
        {
            LoadingQuote = false;
        }
    }

    private async Task LoadDashboardDataAsync()
    {
        IsLoadingRecent = true;
        try
        {
            var allArticles = await _articleApi.GetRootArticlesAsync();
            RecentArticles = await GetRecentArticlesRecursiveAsync(allArticles);

            Stats = new CampaignStats
            {
                TotalArticles = CountTotalArticles(allArticles),
                RootArticles = allArticles.Count,
                RecentlyModified = RecentArticles.Count(a =>
                    (a.ModifiedAt ?? a.CreatedAt) > DateTime.Now.AddDays(-7)),
                DaysSinceStart = allArticles.Any()
                    ? (int)(DateTime.Now - allArticles.Min(a => a.CreatedAt)).TotalDays
                    : 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading dashboard data");
        }
        finally
        {
            IsLoadingRecent = false;
        }
    }

    private async Task<List<ArticleDto>> GetRecentArticlesRecursiveAsync(
        List<ArticleTreeDto> articles,
        int maxResults = 5)
    {
        var allArticles = new List<ArticleDto>();

        foreach (var node in articles)
        {
            var full = await _articleApi.GetArticleAsync(node.Id);
            if (full != null)
                allArticles.Add(full);

            if (node.HasChildren && node.Children != null)
            {
                var childResults = await GetRecentArticlesRecursiveAsync(
                    node.Children.ToList(), maxResults);
                allArticles.AddRange(childResults);
            }
        }

        return allArticles
            .OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt)
            .Take(maxResults)
            .ToList();
    }

    private static int CountTotalArticles(List<ArticleTreeDto> articles)
    {
        int count = articles.Count;
        foreach (var article in articles)
        {
            if (article.Children != null)
                count += CountTotalArticles(article.Children.ToList());
        }
        return count;
    }

    private void OnTreeStateChanged() => RaisePropertyChanged(nameof(Stats));

    /// <inheritdoc />
    public void Dispose()
    {
        _treeState.OnStateChanged -= OnTreeStateChanged;
    }

    // -------------------------------------------------------------------------
    // Nested types
    // -------------------------------------------------------------------------

    /// <summary>Aggregated campaign statistics shown in the stat cards.</summary>
    public sealed record CampaignStats
    {
        /// <summary>Total number of articles across all levels of the hierarchy.</summary>
        public int TotalArticles { get; init; }

        /// <summary>Number of top-level (root) articles.</summary>
        public int RootArticles { get; init; }

        /// <summary>Number of articles modified in the last 7 days.</summary>
        public int RecentlyModified { get; init; }

        /// <summary>Days since the oldest article was created.</summary>
        public int DaysSinceStart { get; init; }
    }
}
