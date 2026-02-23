using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the ArcDetail page.
/// Manages loading, editing, session creation, and quest selection for a single arc.
/// </summary>
public sealed class ArcDetailViewModel : ViewModelBase
{
    private readonly IArcApiService _arcApi;
    private readonly ICampaignApiService _campaignApi;
    private readonly IWorldApiService _worldApi;
    private readonly IArticleApiService _articleApi;
    private readonly IQuestApiService _questApi;
    private readonly IAuthService _authService;
    private readonly ITreeStateService _treeState;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly IPageTitleService _titleService;
    private readonly IConfirmationService _confirmation;
    private readonly ILogger<ArcDetailViewModel> _logger;

    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isTogglingActive;
    private bool _hasUnsavedChanges;
    private bool _summaryExpanded;
    private ArcDto? _arc;
    private CampaignDto? _campaign;
    private List<ArticleTreeDto> _sessions = new();
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private int _editSortOrder;
    private List<BreadcrumbItem> _breadcrumbs = new();
    private QuestDto? _selectedQuest;
    private Guid _currentUserId;
    private bool _isCurrentUserGM;

    public ArcDetailViewModel(
        IArcApiService arcApi,
        ICampaignApiService campaignApi,
        IWorldApiService worldApi,
        IArticleApiService articleApi,
        IQuestApiService questApi,
        IAuthService authService,
        ITreeStateService treeState,
        IBreadcrumbService breadcrumbService,
        IAppNavigator navigator,
        IUserNotifier notifier,
        IPageTitleService titleService,
        IConfirmationService confirmation,
        ILogger<ArcDetailViewModel> logger)
    {
        _arcApi = arcApi;
        _campaignApi = campaignApi;
        _worldApi = worldApi;
        _articleApi = articleApi;
        _questApi = questApi;
        _authService = authService;
        _treeState = treeState;
        _breadcrumbService = breadcrumbService;
        _navigator = navigator;
        _notifier = notifier;
        _titleService = titleService;
        _confirmation = confirmation;
        _logger = logger;
    }

    public bool IsLoading { get => _isLoading; private set => SetField(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetField(ref _isSaving, value); }
    public bool IsTogglingActive { get => _isTogglingActive; private set => SetField(ref _isTogglingActive, value); }
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetField(ref _hasUnsavedChanges, value); }
    public bool SummaryExpanded { get => _summaryExpanded; set => SetField(ref _summaryExpanded, value); }
    public ArcDto? Arc { get => _arc; private set => SetField(ref _arc, value); }
    public CampaignDto? Campaign { get => _campaign; private set => SetField(ref _campaign, value); }
    public List<ArticleTreeDto> Sessions { get => _sessions; private set => SetField(ref _sessions, value); }
    public List<BreadcrumbItem> Breadcrumbs { get => _breadcrumbs; private set => SetField(ref _breadcrumbs, value); }
    public QuestDto? SelectedQuest { get => _selectedQuest; private set => SetField(ref _selectedQuest, value); }
    public Guid CurrentUserId { get => _currentUserId; private set => SetField(ref _currentUserId, value); }
    public bool IsCurrentUserGM { get => _isCurrentUserGM; private set => SetField(ref _isCurrentUserGM, value); }
    public int EditSortOrder { get => _editSortOrder; set { if (SetField(ref _editSortOrder, value)) HasUnsavedChanges = true; } }

    public string EditName
    {
        get => _editName;
        set { if (SetField(ref _editName, value)) HasUnsavedChanges = true; }
    }

    public string EditDescription
    {
        get => _editDescription;
        set { if (SetField(ref _editDescription, value)) HasUnsavedChanges = true; }
    }

    /// <summary>Loads the arc and all related data for the given <paramref name="arcId"/>.</summary>
    public async Task LoadAsync(Guid arcId)
    {
        IsLoading = true;

        try
        {
            var arc = await _arcApi.GetArcAsync(arcId);
            if (arc == null)
            {
                _navigator.NavigateTo("/dashboard", replace: true);
                return;
            }

            Arc = arc;
            EditName = arc.Name;
            EditDescription = arc.Description ?? string.Empty;
            EditSortOrder = arc.SortOrder;
            HasUnsavedChanges = false;

            var allArticles = await _articleApi.GetAllArticlesAsync();
            Sessions = allArticles
                .Where(a => a.ArcId == arcId && a.Type == ArticleType.Session)
                .ToList();

            var campaign = await _campaignApi.GetCampaignAsync(arc.CampaignId);
            Campaign = campaign;

            WorldDetailDto? world = null;
            if (campaign != null)
                world = await _worldApi.GetWorldAsync(campaign.WorldId);

            Breadcrumbs = (arc != null && campaign != null && world != null)
                ? _breadcrumbService.ForArc(arc, campaign, world)
                : new List<BreadcrumbItem>
                {
                    new("Dashboard", href: "/dashboard"),
                    new(arc?.Name ?? "Arc", href: null, disabled: true)
                };

            await _titleService.SetTitleAsync(arc.Name);
            _treeState.ExpandPathToAndSelect(arcId);

            // Resolve GM status
            var user = await _authService.GetCurrentUserAsync();
            if (user != null && world != null)
            {
                var worldDetail = await _worldApi.GetWorldAsync(world.Id);
                if (worldDetail?.Members != null)
                {
                    var member = worldDetail.Members.FirstOrDefault(m =>
                        m.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

                    if (member != null)
                    {
                        CurrentUserId = member.UserId;
                        IsCurrentUserGM = member.Role == WorldRole.GM;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading arc {ArcId}", arcId);
            _notifier.Error($"Failed to load arc: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Toggles the active state of the arc.</summary>
    public async Task OnActiveToggleAsync(bool isActive)
    {
        if (_arc == null || IsTogglingActive)
            return;

        IsTogglingActive = true;

        try
        {
            if (isActive)
            {
                var success = await _arcApi.ActivateArcAsync(_arc.Id);
                if (success)
                {
                    _arc.IsActive = true;
                    RaisePropertyChanged(nameof(Arc));
                    _notifier.Success("Arc set as active");
                }
                else
                {
                    _notifier.Error("Failed to activate arc");
                }
            }
            else
            {
                _notifier.Info("To deactivate, set another arc as active");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error toggling arc active state");
            _notifier.Error($"Error: {ex.Message}");
        }
        finally
        {
            IsTogglingActive = false;
        }
    }

    /// <summary>Persists name, description, and sort order changes to the API.</summary>
    public async Task SaveAsync()
    {
        if (_arc == null || IsSaving)
            return;

        IsSaving = true;

        try
        {
            var updateDto = new ArcUpdateDto
            {
                Name = EditName.Trim(),
                Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                SortOrder = EditSortOrder
            };

            var updated = await _arcApi.UpdateArcAsync(_arc.Id, updateDto);
            if (updated != null)
            {
                _arc.Name = updated.Name;
                _arc.Description = updated.Description;
                _arc.SortOrder = updated.SortOrder;
                HasUnsavedChanges = false;

                await _treeState.RefreshAsync();
                await _titleService.SetTitleAsync(EditName);
                _notifier.Success("Arc saved");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error saving arc");
            _notifier.Error($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Confirms and deletes the arc, then navigates back to the campaign.</summary>
    public async Task DeleteAsync()
    {
        if (_arc == null || Sessions.Any())
            return;

        var confirmed = await _confirmation.ConfirmAsync(
            "Delete Arc",
            $"Are you sure you want to delete '{_arc.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        try
        {
            await _arcApi.DeleteArcAsync(_arc.Id);
            await _treeState.RefreshAsync();
            _notifier.Success("Arc deleted");
            _navigator.NavigateTo($"/campaign/{_arc.CampaignId}");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error deleting arc");
            _notifier.Error($"Failed to delete: {ex.Message}");
        }
    }

    /// <summary>Creates a new session article under this arc and navigates to it.</summary>
    public async Task CreateSessionAsync()
    {
        if (_arc == null || _campaign == null)
            return;

        var sessionNumber = Sessions.Count + 1;
        var createDto = new ArticleCreateDto
        {
            Title = $"Session {sessionNumber}",
            Body = string.Empty,
            WorldId = _campaign.WorldId,
            CampaignId = _campaign.Id,
            ArcId = _arc.Id,
            Type = ArticleType.Session,
            EffectiveDate = DateTime.Now
        };

        try
        {
            var created = await _articleApi.CreateArticleAsync(createDto);
            if (created == null)
            {
                _notifier.Error("Failed to create session");
                return;
            }

            await _treeState.RefreshAsync();
            await LoadAsync(_arc.Id);

            var path = created.Breadcrumbs.Any()
                ? _breadcrumbService.BuildArticleUrl(created.Breadcrumbs)
                : $"/article/{created.Slug}";

            _navigator.NavigateTo(path);
            _notifier.Success("Session created");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error creating session");
            _notifier.Error($"Failed to create session: {ex.Message}");
        }
    }

    /// <summary>Navigates to a session by resolving its full article path.</summary>
    public async Task NavigateToSessionAsync(ArticleTreeDto session)
    {
        try
        {
            var article = await _articleApi.GetArticleDetailAsync(session.Id);
            if (article != null && article.Breadcrumbs.Any())
            {
                var path = _breadcrumbService.BuildArticleUrl(article.Breadcrumbs);
                _navigator.NavigateTo(path);
            }
            else
            {
                _navigator.NavigateTo($"/article/{session.Slug}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error navigating to session {SessionId}", session.Id);
            _notifier.Error("Failed to navigate to session");
        }
    }

    /// <summary>Sets the currently selected quest for the editor panel.</summary>
    public void OnEditQuest(QuestDto quest) => SelectedQuest = quest;

    /// <summary>Updates the selected quest after an edit.</summary>
    public void OnQuestUpdated(QuestDto updatedQuest) => SelectedQuest = updatedQuest;
}
