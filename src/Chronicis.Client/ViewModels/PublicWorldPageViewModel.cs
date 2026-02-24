using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.JSInterop;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the public (unauthenticated) world viewer page.
/// Owns world/article loading, breadcrumb building, page-title computation,
/// and the JS interop lifecycle for wiki-link click handling.
/// Implements <see cref="IAsyncDisposable"/> to dispose the
/// <see cref="DotNetObjectReference{T}"/> created for JS interop.
/// </summary>
public sealed class PublicWorldPageViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly IPublicApiService _publicApi;
    private readonly IAppNavigator _navigator;
    private readonly ILogger<PublicWorldPageViewModel> _logger;

    private WorldDetailDto? _world;
    private List<ArticleTreeDto> _articleTree = new();
    private ArticleDto? _currentArticle;
    private bool _isLoading = true;
    private bool _isLoadingArticle = false;
    private bool _wikiLinksInitialized = false;
    private DotNetObjectReference<PublicWorldPageViewModel>? _dotNetHelper;

    // -------------------------------------------------------------------------
    // Observable properties
    // -------------------------------------------------------------------------

    /// <summary>The loaded world, or <c>null</c> while loading or if not found.</summary>
    public WorldDetailDto? World
    {
        get => _world;
        private set => SetField(ref _world, value);
    }

    /// <summary>The public article tree for the current world.</summary>
    public List<ArticleTreeDto> ArticleTree
    {
        get => _articleTree;
        private set => SetField(ref _articleTree, value);
    }

    /// <summary>The currently displayed article, or <c>null</c> on the world landing view.</summary>
    public ArticleDto? CurrentArticle
    {
        get => _currentArticle;
        private set => SetField(ref _currentArticle, value);
    }

    /// <summary>Whether the initial world load is in progress.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Whether an individual article is currently loading.</summary>
    public bool IsLoadingArticle
    {
        get => _isLoadingArticle;
        private set => SetField(ref _isLoadingArticle, value);
    }

    /// <summary>
    /// Whether wiki-link JS handlers have been initialised for the current article render.
    /// Reset to <c>false</c> on every navigation so <c>OnAfterRenderAsync</c> re-initialises them.
    /// </summary>
    public bool WikiLinksInitialized
    {
        get => _wikiLinksInitialized;
        private set => SetField(ref _wikiLinksInitialized, value);
    }

    public PublicWorldPageViewModel(
        IPublicApiService publicApi,
        IAppNavigator navigator,
        ILogger<PublicWorldPageViewModel> logger)
    {
        _publicApi = publicApi;
        _navigator = navigator;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads world + tree and, if an article path is supplied, the article too.
    /// Resets wiki-link init flag so <c>OnAfterRenderAsync</c> re-binds on navigation.
    /// Call from <c>OnParametersSetAsync</c>.
    /// </summary>
    public async Task LoadWorldAsync(string publicSlug, string? articlePath)
    {
        WikiLinksInitialized = false;
        IsLoading = true;

        try
        {
            World = await _publicApi.GetPublicWorldAsync(publicSlug);

            if (World != null)
            {
                ArticleTree = await _publicApi.GetPublicArticleTreeAsync(publicSlug);

                if (!string.IsNullOrEmpty(articlePath))
                {
                    // Switch from page-level loading to article-level skeleton state.
                    IsLoading = false;
                    await LoadArticleAsync(publicSlug, articlePath);
                }
                else
                    CurrentArticle = null;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Initialises the JS wiki-link click handlers if they have not yet been set up for the current render.
    /// Call from <c>OnAfterRenderAsync</c> when <see cref="CurrentArticle"/> has body content.
    /// </summary>
    public async Task InitializeWikiLinksAsync(IJSRuntime jsRuntime)
    {
        if (WikiLinksInitialized)
            return;

        _dotNetHelper ??= DotNetObjectReference.Create(this);
        try
        {
            await jsRuntime.InvokeAsync<object>(
                "initializePublicWikiLinks", "public-article-body", _dotNetHelper);
            WikiLinksInitialized = true;
        }
        catch (Exception)
        {
            // Intentionally swallowed — JS interop may fail during pre-rendering
            // or if the component is disposed before the call completes.
        }
    }

    /// <summary>
    /// Invoked from JavaScript when a wiki link in the public article body is clicked.
    /// Resolves the article path and navigates to it.
    /// </summary>
    [JSInvokable]
    public async Task OnPublicWikiLinkClicked(string targetArticleId)
    {
        if (!Guid.TryParse(targetArticleId, out var articleId))
            return;

        // publicSlug must be captured; read from the current world.
        var slug = World?.Slug ?? string.Empty;
        if (string.IsNullOrEmpty(slug))
            return;

        try
        {
            var path = await _publicApi.ResolvePublicArticlePathAsync(slug, articleId);
            if (!string.IsNullOrEmpty(path))
                _navigator.NavigateTo($"/w/{slug}/{path}");
        }
        catch (Exception)
        {
            // Silently ignore — link may not be public or may not exist.
        }
    }

    // -------------------------------------------------------------------------
    // Navigation helpers (called by the component's event callbacks)
    // -------------------------------------------------------------------------

    /// <summary>Navigates to <paramref name="articlePath"/> within the current world.</summary>
    public void NavigateToArticle(string publicSlug, string articlePath)
    {
        if (string.IsNullOrEmpty(articlePath))
            _navigator.NavigateTo($"/w/{publicSlug}");
        else
            _navigator.NavigateTo($"/w/{publicSlug}/{articlePath}");
    }

    // -------------------------------------------------------------------------
    // Pure helpers
    // -------------------------------------------------------------------------

    /// <summary>Builds breadcrumb items for the current article.</summary>
    public List<BreadcrumbItem> GetBreadcrumbItems(string publicSlug)
    {
        return CurrentArticle == null
            ? new List<BreadcrumbItem>()
            : PublicBreadcrumbBuilder.Build(publicSlug, CurrentArticle);
    }

    /// <summary>Returns the page title, incorporating the world name when loaded.</summary>
    public string GetPageTitle()
    {
        var worldName = string.IsNullOrWhiteSpace(World?.Name) ? "World" : World.Name;
        return $"{worldName} — Chronicis";
    }

    /// <summary>Maps an <see cref="ArticleType"/> to its display label.</summary>
    public static string GetArticleTypeLabel(ArticleType type) => type switch
    {
        ArticleType.WikiArticle => "Wiki Article",
        ArticleType.Character => "Character",
        ArticleType.CharacterNote => "Character Note",
        ArticleType.Session => "Session",
        ArticleType.SessionNote => "Session Note",
        ArticleType.Legacy => "Article",
        _ => "Article",
    };

    // -------------------------------------------------------------------------
    // IAsyncDisposable
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _dotNetHelper?.Dispose();
        return ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task LoadArticleAsync(string publicSlug, string articlePath)
    {
        IsLoadingArticle = true;
        try
        {
            CurrentArticle = await _publicApi.GetPublicArticleAsync(publicSlug, articlePath);
        }
        finally
        {
            IsLoadingArticle = false;
        }
    }
}
