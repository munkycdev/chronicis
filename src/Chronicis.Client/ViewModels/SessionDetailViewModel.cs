using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the Session detail page.
/// </summary>
public sealed class SessionDetailViewModel : ViewModelBase
{
    private readonly ISessionApiService _sessionApi;
    private readonly IArticleApiService _articleApi;
    private readonly IArcApiService _arcApi;
    private readonly ICampaignApiService _campaignApi;
    private readonly IWorldApiService _worldApi;
    private readonly IAuthService _authService;
    private readonly ITreeStateService _treeState;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly IPageTitleService _titleService;
    private readonly ILogger<SessionDetailViewModel> _logger;

    private bool _isLoading = true;
    private bool _isSavingNotes;
    private bool _isGeneratingSummary;
    private bool _hasUnsavedChanges;
    private SessionDto? _session;
    private ArcDto? _arc;
    private CampaignDetailDto? _campaign;
    private WorldDetailDto? _world;
    private List<ArticleTreeDto> _sessionNotes = new();
    private List<BreadcrumbItem> _breadcrumbs = new();
    private bool _isCurrentUserGm;
    private Guid _currentUserId;
    private string _editName = string.Empty;
    private DateTime? _editSessionDate;
    private string _editPublicNotes = string.Empty;
    private string _editPrivateNotes = string.Empty;

    public SessionDetailViewModel(
        ISessionApiService sessionApi,
        IArticleApiService articleApi,
        IArcApiService arcApi,
        ICampaignApiService campaignApi,
        IWorldApiService worldApi,
        IAuthService authService,
        ITreeStateService treeState,
        IBreadcrumbService breadcrumbService,
        IAppNavigator navigator,
        IUserNotifier notifier,
        IPageTitleService titleService,
        ILogger<SessionDetailViewModel> logger)
    {
        _sessionApi = sessionApi;
        _articleApi = articleApi;
        _arcApi = arcApi;
        _campaignApi = campaignApi;
        _worldApi = worldApi;
        _authService = authService;
        _treeState = treeState;
        _breadcrumbService = breadcrumbService;
        _navigator = navigator;
        _notifier = notifier;
        _titleService = titleService;
        _logger = logger;
    }

    public bool IsLoading { get => _isLoading; private set => SetField(ref _isLoading, value); }
    public bool IsSavingNotes { get => _isSavingNotes; private set => SetField(ref _isSavingNotes, value); }
    public bool IsGeneratingSummary { get => _isGeneratingSummary; private set => SetField(ref _isGeneratingSummary, value); }
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetField(ref _hasUnsavedChanges, value); }
    public SessionDto? Session { get => _session; private set => SetField(ref _session, value); }
    public ArcDto? Arc { get => _arc; private set => SetField(ref _arc, value); }
    public CampaignDetailDto? Campaign { get => _campaign; private set => SetField(ref _campaign, value); }
    public WorldDetailDto? World { get => _world; private set => SetField(ref _world, value); }
    public List<ArticleTreeDto> SessionNotes { get => _sessionNotes; private set => SetField(ref _sessionNotes, value); }
    public List<BreadcrumbItem> Breadcrumbs { get => _breadcrumbs; private set => SetField(ref _breadcrumbs, value); }
    public bool IsCurrentUserGM { get => _isCurrentUserGm; private set => SetField(ref _isCurrentUserGm, value); }
    public Guid CurrentUserId { get => _currentUserId; private set => SetField(ref _currentUserId, value); }

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetField(ref _editName, value) && Session != null && IsCurrentUserGM)
            {
                UpdateDirtyState();
            }
        }
    }

    public DateTime? EditSessionDate
    {
        get => _editSessionDate;
        set
        {
            if (SetField(ref _editSessionDate, value) && Session != null && IsCurrentUserGM)
            {
                UpdateDirtyState();
            }
        }
    }

    public string EditPublicNotes
    {
        get => _editPublicNotes;
        set
        {
            if (SetField(ref _editPublicNotes, value) && Session != null && IsCurrentUserGM)
            {
                UpdateDirtyState();
            }
        }
    }

    public string EditPrivateNotes
    {
        get => _editPrivateNotes;
        set
        {
            if (SetField(ref _editPrivateNotes, value) && Session != null && IsCurrentUserGM)
            {
                UpdateDirtyState();
            }
        }
    }

    public async Task LoadAsync(Guid sessionId)
    {
        IsLoading = true;

        try
        {
            var session = await _sessionApi.GetSessionAsync(sessionId);
            if (session == null)
            {
                _navigator.NavigateTo("/dashboard", replace: true);
                return;
            }

            Session = session;
            EditName = session.Name;
            EditSessionDate = session.SessionDate?.Date;
            EditPublicNotes = session.PublicNotes ?? string.Empty;
            EditPrivateNotes = session.PrivateNotes ?? string.Empty;
            HasUnsavedChanges = false;

            _treeState.ExpandPathToAndSelect(sessionId);

            Arc = await _arcApi.GetArcAsync(session.ArcId);
            if (Arc != null)
            {
                Campaign = await _campaignApi.GetCampaignAsync(Arc.CampaignId);
                if (Campaign != null)
                {
                    World = await _worldApi.GetWorldAsync(Campaign.WorldId);
                }
            }

            await ResolveCurrentUserRoleAsync();
            Breadcrumbs = BuildBreadcrumbs();
            await LoadSessionNotesAsync(sessionId);
            await _titleService.SetTitleAsync(session.Name);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading session {SessionId}", sessionId);
            _notifier.Error($"Failed to load session: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveNotesAsync()
    {
        if (Session == null || !IsCurrentUserGM || IsSavingNotes)
        {
            return;
        }

        var trimmedName = (EditName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            _notifier.Error("Session title is required");
            return;
        }

        if (trimmedName.Length > 500)
        {
            _notifier.Error("Session title must be 500 characters or fewer");
            return;
        }

        var editedSessionDate = EditSessionDate?.Date;
        var shouldClearSessionDate = Session.SessionDate.HasValue && !editedSessionDate.HasValue;

        IsSavingNotes = true;

        try
        {
            var updated = await _sessionApi.UpdateSessionNotesAsync(Session.Id, new SessionUpdateDto
            {
                Name = trimmedName,
                SessionDate = editedSessionDate,
                ClearSessionDate = shouldClearSessionDate,
                PublicNotes = string.IsNullOrWhiteSpace(EditPublicNotes) ? null : EditPublicNotes,
                PrivateNotes = string.IsNullOrWhiteSpace(EditPrivateNotes) ? null : EditPrivateNotes
            });

            if (updated == null)
            {
                _notifier.Error("Failed to save session notes");
                return;
            }

            Session = updated;
            EditName = updated.Name;
            EditSessionDate = updated.SessionDate?.Date;
            EditPublicNotes = updated.PublicNotes ?? string.Empty;
            EditPrivateNotes = updated.PrivateNotes ?? string.Empty;
            Breadcrumbs = BuildBreadcrumbs();
            await _titleService.SetTitleAsync(updated.Name);
            await _treeState.RefreshAsync();
            HasUnsavedChanges = false;
            _notifier.Success("Session saved");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error saving session {SessionId}", Session.Id);
            _notifier.Error($"Failed to save session: {ex.Message}");
        }
        finally
        {
            IsSavingNotes = false;
        }
    }

    public async Task GenerateAiSummaryAsync()
    {
        if (Session == null || IsGeneratingSummary)
        {
            return;
        }

        IsGeneratingSummary = true;

        try
        {
            var result = await _sessionApi.GenerateAiSummaryAsync(Session.Id);
            if (result?.Success != true)
            {
                _notifier.Error(result?.ErrorMessage ?? "Failed to generate AI summary");
                return;
            }

            Session.AiSummary = result.Summary;
            Session.AiSummaryGeneratedAt = result.GeneratedDate;
            RaisePropertyChanged(nameof(Session));

            _notifier.Success("AI summary generated");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error generating AI summary for session {SessionId}", Session.Id);
            _notifier.Error($"Failed to generate summary: {ex.Message}");
        }
        finally
        {
            IsGeneratingSummary = false;
        }
    }

    public async Task OpenSessionNoteAsync(ArticleTreeDto note)
    {
        try
        {
            var article = await _articleApi.GetArticleDetailAsync(note.Id);
            if (article != null && article.Breadcrumbs.Any())
            {
                _navigator.NavigateTo(_breadcrumbService.BuildArticleUrl(article.Breadcrumbs));
                return;
            }

            _navigator.NavigateTo($"/article/{note.Slug}");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error navigating to session note {ArticleId}", note.Id);
            _notifier.Error("Failed to navigate to session note");
        }
    }

    private async Task LoadSessionNotesAsync(Guid sessionId)
    {
        var allArticles = await _articleApi.GetAllArticlesAsync();
        SessionNotes = allArticles
            .Where(a => a.Type == ArticleType.SessionNote && a.SessionId == sessionId)
            .OrderBy(a => a.Title)
            .ThenBy(a => a.CreatedAt)
            .ToList();
    }

    private void UpdateDirtyState()
    {
        if (Session == null || !IsCurrentUserGM)
        {
            HasUnsavedChanges = false;
            return;
        }

        var nameChanged = !string.Equals(Session.Name ?? string.Empty, EditName ?? string.Empty, StringComparison.Ordinal);
        var dateChanged = !AreSameDate(Session.SessionDate, EditSessionDate);
        var publicChanged = !string.Equals(Session.PublicNotes ?? string.Empty, EditPublicNotes ?? string.Empty, StringComparison.Ordinal);
        var privateChanged = !string.Equals(Session.PrivateNotes ?? string.Empty, EditPrivateNotes ?? string.Empty, StringComparison.Ordinal);
        HasUnsavedChanges = nameChanged || dateChanged || publicChanged || privateChanged;
    }

    private async Task ResolveCurrentUserRoleAsync()
    {
        IsCurrentUserGM = false;
        CurrentUserId = Guid.Empty;

        if (World == null)
        {
            return;
        }

        var user = await _authService.GetCurrentUserAsync();
        if (user == null || World.Members == null)
        {
            return;
        }

        var member = World.Members.FirstOrDefault(m =>
            m.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

        if (member == null)
        {
            return;
        }

        CurrentUserId = member.UserId;
        IsCurrentUserGM = member.Role == WorldRole.GM;
    }

    private List<BreadcrumbItem> BuildBreadcrumbs()
    {
        if (Session == null)
        {
            return new List<BreadcrumbItem> { new("Dashboard", href: "/dashboard") };
        }

        if (Arc != null && Campaign != null && World != null)
        {
            var items = _breadcrumbService.ForArc(Arc, Campaign, World, currentDisabled: false);
            items.Add(new BreadcrumbItem(Session.Name, href: null, disabled: true));
            return items;
        }

        return new List<BreadcrumbItem>
        {
            new("Dashboard", href: "/dashboard"),
            new(Session.Name, href: null, disabled: true)
        };
    }

    private static bool AreSameDate(DateTime? left, DateTime? right)
        => left?.Date == right?.Date;
}
