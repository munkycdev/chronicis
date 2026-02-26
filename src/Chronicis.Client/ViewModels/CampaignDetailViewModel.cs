using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for the CampaignDetail page.
/// Manages loading, editing, saving, and arc creation for a single campaign.
/// </summary>
public sealed class CampaignDetailViewModel : ViewModelBase
{
    private readonly ICampaignApiService _campaignApi;
    private readonly IArcApiService _arcApi;
    private readonly IWorldApiService _worldApi;
    private readonly IAuthService _authService;
    private readonly ITreeStateService _treeState;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly IPageTitleService _titleService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CampaignDetailViewModel> _logger;

    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isTogglingActive;
    private bool _hasUnsavedChanges;
    private bool _summaryExpanded;
    private CampaignDetailDto? _campaign;
    private List<ArcDto> _arcs = new();
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editPrivateNotes = string.Empty;
    private List<BreadcrumbItem> _breadcrumbs = new();
    private bool _isCurrentUserGm;
    private bool _isCurrentUserWorldOwner;

    public CampaignDetailViewModel(
        ICampaignApiService campaignApi,
        IArcApiService arcApi,
        IWorldApiService worldApi,
        IAuthService authService,
        ITreeStateService treeState,
        IBreadcrumbService breadcrumbService,
        IAppNavigator navigator,
        IUserNotifier notifier,
        IPageTitleService titleService,
        IDialogService dialogService,
        ILogger<CampaignDetailViewModel> logger)
    {
        _campaignApi = campaignApi;
        _arcApi = arcApi;
        _worldApi = worldApi;
        _authService = authService;
        _treeState = treeState;
        _breadcrumbService = breadcrumbService;
        _navigator = navigator;
        _notifier = notifier;
        _titleService = titleService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public bool IsLoading { get => _isLoading; private set => SetField(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetField(ref _isSaving, value); }
    public bool IsTogglingActive { get => _isTogglingActive; private set => SetField(ref _isTogglingActive, value); }
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetField(ref _hasUnsavedChanges, value); }
    public bool SummaryExpanded { get => _summaryExpanded; set => SetField(ref _summaryExpanded, value); }
    public CampaignDetailDto? Campaign { get => _campaign; private set => SetField(ref _campaign, value); }
    public List<ArcDto> Arcs { get => _arcs; private set => SetField(ref _arcs, value); }
    public List<BreadcrumbItem> Breadcrumbs { get => _breadcrumbs; private set => SetField(ref _breadcrumbs, value); }
    public bool IsCurrentUserGM { get => _isCurrentUserGm; private set => SetField(ref _isCurrentUserGm, value); }
    public bool IsCurrentUserWorldOwner { get => _isCurrentUserWorldOwner; private set => SetField(ref _isCurrentUserWorldOwner, value); }
    public bool CanManageCampaignDetails => IsCurrentUserGM || IsCurrentUserWorldOwner;
    public bool CanViewPrivateNotes => CanManageCampaignDetails;

    public string EditName
    {
        get => _editName;
        set
        {
            if (SetField(ref _editName, value))
                HasUnsavedChanges = true;
        }
    }

    public string EditDescription
    {
        get => _editDescription;
        set
        {
            if (SetField(ref _editDescription, value))
                HasUnsavedChanges = true;
        }
    }

    public string EditPrivateNotes
    {
        get => _editPrivateNotes;
        set
        {
            if (SetField(ref _editPrivateNotes, value) && CanManageCampaignDetails)
                HasUnsavedChanges = true;
        }
    }

    /// <summary>Loads the campaign and all related data for the given <paramref name="campaignId"/>.</summary>
    public async Task LoadAsync(Guid campaignId)
    {
        IsLoading = true;

        try
        {
            IsCurrentUserGM = false;
            IsCurrentUserWorldOwner = false;

            var campaign = await _campaignApi.GetCampaignAsync(campaignId);
            if (campaign == null)
            {
                _navigator.NavigateTo("/dashboard", replace: true);
                return;
            }

            Campaign = campaign;
            EditName = campaign.Name;
            EditDescription = campaign.Description ?? string.Empty;
            EditPrivateNotes = campaign.PrivateNotes ?? string.Empty;
            HasUnsavedChanges = false;

            Arcs = await _arcApi.GetArcsByCampaignAsync(campaignId);

            var world = await _worldApi.GetWorldAsync(campaign.WorldId);
            Breadcrumbs = world != null
                ? _breadcrumbService.ForCampaign(campaign, world)
                : new List<BreadcrumbItem>
                {
                    new("Dashboard", href: "/dashboard"),
                    new(campaign.Name, href: null, disabled: true)
                };

            await ResolveCurrentUserRoleAsync(world);

            await _titleService.SetTitleAsync(campaign.Name);
            _treeState.ExpandPathToAndSelect(campaignId);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading campaign {CampaignId}", campaignId);
            _notifier.Error($"Failed to load campaign: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Toggles the active state of the campaign.</summary>
    public async Task OnActiveToggleAsync(bool isActive)
    {
        if (_campaign == null || !CanManageCampaignDetails || IsTogglingActive)
            return;

        IsTogglingActive = true;

        try
        {
            if (isActive)
            {
                var success = await _campaignApi.ActivateCampaignAsync(_campaign.Id);
                if (success)
                {
                    _campaign.IsActive = true;
                    RaisePropertyChanged(nameof(Campaign));
                    _notifier.Success("Campaign set as active");
                }
                else
                {
                    _notifier.Error("Failed to activate campaign");
                }
            }
            else
            {
                _notifier.Info("To deactivate, set another campaign as active");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error toggling campaign active state");
            _notifier.Error($"Error: {ex.Message}");
        }
        finally
        {
            IsTogglingActive = false;
        }
    }

    /// <summary>Persists name and description changes to the API.</summary>
    public async Task SaveAsync()
    {
        if (_campaign == null || !CanManageCampaignDetails || IsSaving)
            return;

        IsSaving = true;

        try
        {
            var updateDto = new CampaignUpdateDto
            {
                Name = EditName.Trim(),
                Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                PrivateNotes = string.IsNullOrWhiteSpace(EditPrivateNotes) ? null : EditPrivateNotes
            };

            var updated = await _campaignApi.UpdateCampaignAsync(_campaign.Id, updateDto);
            if (updated != null)
            {
                _campaign.Name = updated.Name;
                _campaign.Description = updated.Description;
                _campaign.PrivateNotes = string.IsNullOrWhiteSpace(EditPrivateNotes) ? null : EditPrivateNotes;
                HasUnsavedChanges = false;

                await _treeState.RefreshAsync();
                await _titleService.SetTitleAsync(EditName);
                _notifier.Success("Campaign saved");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error saving campaign");
            _notifier.Error($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Opens the create-arc dialog and navigates to the new arc on success.</summary>
    public async Task CreateArcAsync()
    {
        if (_campaign == null || !IsCurrentUserGM)
            return;

        var parameters = new DialogParameters { { "CampaignId", _campaign.Id } };
        var dialog = await _dialogService.ShowAsync<CreateArcDialog>("New Arc", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is ArcDto arc)
        {
            await _treeState.RefreshAsync();
            await LoadAsync(_campaign.Id);
            _navigator.NavigateTo($"/arc/{arc.Id}");
            _notifier.Success("Arc created");
        }
    }

    /// <summary>Navigates to the arc detail page.</summary>
    public void NavigateToArc(Guid arcId) => _navigator.NavigateTo($"/arc/{arcId}");

    private async Task ResolveCurrentUserRoleAsync(WorldDetailDto? world)
    {
        IsCurrentUserGM = false;
        IsCurrentUserWorldOwner = false;

        if (world?.Members == null)
        {
            return;
        }

        var user = await _authService.GetCurrentUserAsync();
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var member = world.Members.FirstOrDefault(m =>
            m.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));

        if (member == null)
        {
            return;
        }

        IsCurrentUserGM = member.Role == WorldRole.GM;
        IsCurrentUserWorldOwner = member.UserId == world.OwnerId;
    }
}
