using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page.
/// Coordinates data loading, world ordering, and user actions.
/// Implements <see cref="IDisposable"/> to clean up the tree state subscription.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IDashboardApiService _dashboardApi;
    private readonly IUserApiService _userApi;
    private readonly IWorldApiService _worldApi;
    private readonly IArticleApiService _articleApi;
    private readonly IQuoteService _quoteService;
    private readonly ITreeStateService _treeState;
    private readonly IDialogService _dialogService;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<DashboardViewModel> _logger;

    private bool _isLoading = true;
    private DashboardDto? _dashboard;
    private List<DashboardWorldDto> _orderedWorlds = new();
    private Quote? _quote;

    public DashboardViewModel(
        IDashboardApiService dashboardApi,
        IUserApiService userApi,
        IWorldApiService worldApi,
        IArticleApiService articleApi,
        IQuoteService quoteService,
        ITreeStateService treeState,
        IDialogService dialogService,
        IAppNavigator navigator,
        IUserNotifier notifier,
        ILogger<DashboardViewModel> logger)
    {
        _dashboardApi = dashboardApi;
        _userApi = userApi;
        _worldApi = worldApi;
        _articleApi = articleApi;
        _quoteService = quoteService;
        _treeState = treeState;
        _dialogService = dialogService;
        _navigator = navigator;
        _notifier = notifier;
        _logger = logger;

        _treeState.OnStateChanged += OnTreeStateChanged;
    }

    /// <summary>Whether the initial data load is in progress.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>The loaded dashboard data, or <c>null</c> if loading failed.</summary>
    public DashboardDto? Dashboard
    {
        get => _dashboard;
        private set => SetField(ref _dashboard, value);
    }

    /// <summary>Worlds ordered by activity for display.</summary>
    public List<DashboardWorldDto> OrderedWorlds
    {
        get => _orderedWorlds;
        private set => SetField(ref _orderedWorlds, value);
    }

    /// <summary>The motivational quote shown in the hero section.</summary>
    public Quote? Quote
    {
        get => _quote;
        private set => SetField(ref _quote, value);
    }

    /// <summary>
    /// Runs onboarding redirect check, subscribes to tree state, and loads dashboard + quote in parallel.
    /// </summary>
    public async Task InitializeAsync()
    {
        var profile = await _userApi.GetUserProfileAsync();
        if (profile != null && !profile.HasCompletedOnboarding)
        {
            _navigator.NavigateTo("/getting-started", replace: true);
            return;
        }

        await Task.WhenAll(LoadDashboardAsync(), LoadQuoteAsync());
    }

    /// <summary>Loads (or reloads) the dashboard data and updates <see cref="OrderedWorlds"/>.</summary>
    public async Task LoadDashboardAsync()
    {
        IsLoading = true;

        try
        {
            var data = await _dashboardApi.GetDashboardAsync();
            Dashboard = data;

            if (data != null)
            {
                OrderedWorlds = data.Worlds
                    .OrderByDescending(w => w.Campaigns.Any(c => c.IsActive))
                    .ThenByDescending(w => w.Campaigns
                        .Where(c => c.CurrentArc?.LatestSessionDate != null)
                        .Select(c => c.CurrentArc!.LatestSessionDate)
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max())
                    .ThenByDescending(w => w.CreatedAt)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading dashboard");
            _notifier.Error("Failed to load dashboard");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Loads the motivational quote. Failures are swallowed silently.</summary>
    public async Task LoadQuoteAsync()
    {
        try
        {
            Quote = await _quoteService.GetRandomQuoteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading quote");
        }
    }

    /// <summary>Creates a new world and navigates to it.</summary>
    public async Task CreateNewWorldAsync()
    {
        try
        {
            var createDto = new WorldCreateDto
            {
                Name = "New World",
                Description = "A new world for your adventures"
            };

            var world = await _worldApi.CreateWorldAsync(createDto);

            if (world != null)
            {
                _notifier.Success($"World '{world.Name}' created!");
                await _treeState.RefreshAsync();

                if (world.WorldRootArticleId.HasValue)
                {
                    _treeState.ShouldFocusTitle = true;
                    _navigator.NavigateTo($"/world/{world.Slug}");
                }
                else
                {
                    await LoadDashboardAsync();
                }
            }
            else
            {
                _notifier.Error("Failed to create world");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error creating world");
            _notifier.Error($"Error: {ex.Message}");
        }
    }

    /// <summary>Opens the join-world dialog and handles the result.</summary>
    public async Task JoinWorldAsync()
    {
        var dialog = await _dialogService.ShowAsync<JoinWorldDialog>("Join a World");
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is WorldJoinResultDto joinResult)
        {
            _notifier.Success($"Welcome to {joinResult.WorldName}!");
            await _treeState.RefreshAsync();
            await LoadDashboardAsync();

            if (joinResult.WorldId.HasValue)
            {
                _navigator.NavigateTo($"/world/{joinResult.WorldId}");
            }
        }
    }

    /// <summary>Resolves a character's article path and navigates to it.</summary>
    public async Task NavigateToCharacterAsync(Guid characterId)
    {
        var article = await _articleApi.GetArticleDetailAsync(characterId);
        if (article != null && article.Breadcrumbs.Any())
        {
            var path = string.Join("/", article.Breadcrumbs.Select(b => b.Slug));
            _navigator.NavigateTo($"/article/{path}");
        }
    }

    /// <summary>Handles a dashboard prompt click, navigating if the prompt has an action URL.</summary>
    public void HandlePromptClick(PromptDto prompt)
    {
        if (!string.IsNullOrEmpty(prompt.ActionUrl))
        {
            _navigator.NavigateTo(prompt.ActionUrl);
        }
    }

    /// <summary>Returns the CSS class for a prompt category.</summary>
    public static string GetCategoryClass(PromptCategory category) =>
        category switch
        {
            PromptCategory.MissingFundamental => "missing-fundamental",
            PromptCategory.NeedsAttention => "needs-attention",
            PromptCategory.Suggestion => "suggestion",
            _ => string.Empty
        };

    private void OnTreeStateChanged() => RaisePropertyChanged(nameof(OrderedWorlds));

    /// <inheritdoc />
    public void Dispose()
    {
        _treeState.OnStateChanged -= OnTreeStateChanged;
    }
}
