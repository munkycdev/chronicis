using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// Core ViewModel for the World Detail page.
/// Coordinates loading, saving, and article-creation actions for a world.
/// Sub-ViewModels (WorldLinksViewModel, WorldDocumentsViewModel, WorldSharingViewModel)
/// handle their respective tabs independently.
/// </summary>
public sealed class WorldDetailViewModel : ViewModelBase
{
    private readonly IWorldApiService _worldApi;
    private readonly ITreeStateService _treeState;
    private readonly IBreadcrumbService _breadcrumbService;
    private readonly IDialogService _dialogService;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly IPageTitleService _titleService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<WorldDetailViewModel> _logger;

    private bool _isLoading = true;
    private WorldDetailDto? _world;
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editPrivateNotes = string.Empty;
    private bool _isSaving;
    private bool _hasUnsavedChanges;
    private List<BreadcrumbItem> _breadcrumbs = new();
    private Guid _currentUserId;
    private bool _isCurrentUserGm;
    private bool _isCurrentUserWorldOwner;

    public WorldDetailViewModel(
        IWorldApiService worldApi,
        ITreeStateService treeState,
        IBreadcrumbService breadcrumbService,
        IDialogService dialogService,
        IAppNavigator navigator,
        IUserNotifier notifier,
        IPageTitleService titleService,
        AuthenticationStateProvider authStateProvider,
        ILogger<WorldDetailViewModel> logger)
    {
        _worldApi = worldApi;
        _treeState = treeState;
        _breadcrumbService = breadcrumbService;
        _dialogService = dialogService;
        _navigator = navigator;
        _notifier = notifier;
        _titleService = titleService;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public WorldDetailDto? World
    {
        get => _world;
        private set => SetField(ref _world, value);
    }

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
            if (SetField(ref _editPrivateNotes, value) && CanManageWorldDetails)
                HasUnsavedChanges = true;
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetField(ref _isSaving, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetField(ref _hasUnsavedChanges, value);
    }

    public List<BreadcrumbItem> Breadcrumbs
    {
        get => _breadcrumbs;
        private set => SetField(ref _breadcrumbs, value);
    }

    public Guid CurrentUserId
    {
        get => _currentUserId;
        private set => SetField(ref _currentUserId, value);
    }

    public bool IsCurrentUserGm
    {
        get => _isCurrentUserGm;
        private set => SetField(ref _isCurrentUserGm, value);
    }

    public bool IsCurrentUserWorldOwner
    {
        get => _isCurrentUserWorldOwner;
        private set => SetField(ref _isCurrentUserWorldOwner, value);
    }

    public bool CanManageWorldDetails => IsCurrentUserGm || IsCurrentUserWorldOwner;
    public bool CanViewPrivateNotes => CanManageWorldDetails;

    /// <summary>Loads the world and resolves the current user's role.</summary>
    public async Task LoadAsync(Guid worldId, WorldSharingViewModel sharingVm, WorldLinksViewModel linksVm, WorldDocumentsViewModel documentsVm)
    {
        IsLoading = true;

        try
        {
            CurrentUserId = Guid.Empty;
            IsCurrentUserGm = false;
            IsCurrentUserWorldOwner = false;

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var world = await _worldApi.GetWorldAsync(worldId);

            if (world == null)
            {
                _navigator.NavigateTo("/dashboard", replace: true);
                return;
            }

            World = world;
            EditName = world.Name;
            EditDescription = world.Description ?? string.Empty;
            EditPrivateNotes = world.PrivateNotes ?? string.Empty;
            HasUnsavedChanges = false;
            Breadcrumbs = _breadcrumbService.ForWorld(world);

            // Resolve current user role
            if (world.Members != null)
            {
                var userEmail = authState.User.FindFirst("https://chronicis.app/email")?.Value
                    ?? authState.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? authState.User.FindFirst("email")?.Value;

                if (!string.IsNullOrEmpty(userEmail))
                {
                    var currentMember = world.Members.FirstOrDefault(m =>
                        m.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

                    if (currentMember != null)
                    {
                        CurrentUserId = currentMember.UserId;
                        IsCurrentUserGm = currentMember.Role == WorldRole.GM;
                        IsCurrentUserWorldOwner = world.OwnerId == currentMember.UserId;
                    }
                }
            }

            await _titleService.SetTitleAsync(world.Name);
            _treeState.ExpandPathToAndSelect(worldId);

            // Initialise sub-ViewModels
            sharingVm.InitializeFrom(world);
            await linksVm.LoadAsync(worldId);
            await documentsVm.LoadAsync(worldId);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error loading world {WorldId}", worldId);
            _notifier.Error($"Failed to load world: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Saves name, description, and sharing settings.</summary>
    public async Task SaveAsync(WorldSharingViewModel sharingVm)
    {
        if (_world == null || !CanManageWorldDetails || IsSaving)
            return;

        if (sharingVm.IsPublic && !sharingVm.SlugIsAvailable)
        {
            _notifier.Warning("Please enter a valid, available public slug before saving");
            return;
        }

        IsSaving = true;

        try
        {
            var updateDto = new WorldUpdateDto
            {
                Name = EditName.Trim(),
                Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                PrivateNotes = string.IsNullOrWhiteSpace(EditPrivateNotes) ? null : EditPrivateNotes,
                IsPublic = sharingVm.IsPublic,
                PublicSlug = sharingVm.IsPublic ? sharingVm.PublicSlug.Trim().ToLowerInvariant() : null
            };

            var updated = await _worldApi.UpdateWorldAsync(_world.Id, updateDto);
            if (updated != null)
            {
                _world.Name = updated.Name;
                _world.Description = updated.Description;
                _world.PrivateNotes = string.IsNullOrWhiteSpace(EditPrivateNotes) ? null : EditPrivateNotes;
                _world.IsPublic = updated.IsPublic;
                _world.PublicSlug = updated.PublicSlug;
                HasUnsavedChanges = false;

                await _treeState.RefreshAsync();
                await _titleService.SetTitleAsync(EditName);

                _notifier.Success("World saved");
            }
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error saving world {WorldId}", _world?.Id);
            _notifier.Error($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Reloads member count and list after a membership change.</summary>
    public async Task OnMembersChangedAsync()
    {
        if (_world == null)
            return;

        var updated = await _worldApi.GetWorldAsync(_world.Id);
        if (updated != null)
        {
            _world.MemberCount = updated.MemberCount;
            _world.Members = updated.Members;
            RaisePropertyChanged(nameof(World));
        }
    }

    public async Task CreateCampaignAsync()
    {
        var parameters = new DialogParameters { { "WorldId", _world!.Id } };
        var dialog = await _dialogService.ShowAsync<CreateCampaignDialog>("New Campaign", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is CampaignDto campaign)
        {
            await _treeState.RefreshAsync();
            _navigator.NavigateTo($"/campaign/{campaign.Id}");
            _notifier.Success("Campaign created");
        }
    }

    public async Task CreateCharacterAsync()
    {
        var parameters = new DialogParameters
        {
            { "WorldId", _world!.Id },
            { "ArticleType", ArticleType.Character }
        };

        var dialog = await _dialogService.ShowAsync<CreateArticleDialog>("New Player Character", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is ArticleDto article)
        {
            await _treeState.RefreshAsync();
            NavigateToArticle(article);
            _notifier.Success("Character created");
        }
    }

    public async Task CreateWikiArticleAsync()
    {
        var parameters = new DialogParameters
        {
            { "WorldId", _world!.Id },
            { "ArticleType", ArticleType.WikiArticle }
        };

        var dialog = await _dialogService.ShowAsync<CreateArticleDialog>("New Wiki Article", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is ArticleDto article)
        {
            await _treeState.RefreshAsync();
            NavigateToArticle(article);
            _notifier.Success("Article created");
        }
    }

    private void NavigateToArticle(ArticleDto article)
    {
        if (article.Breadcrumbs != null && article.Breadcrumbs.Any())
        {
            var path = _breadcrumbService.BuildArticleUrl(article.Breadcrumbs);
            _navigator.NavigateTo(path);
        }
        else
        {
            _navigator.NavigateTo($"/article/{article.Slug}");
        }
    }
}
